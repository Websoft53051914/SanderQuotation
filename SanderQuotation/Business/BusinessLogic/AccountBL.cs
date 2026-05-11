using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using Const;
using Core.Utility.Common;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Transactions;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public partial class AccountBL : BaseProjectBL
    {
        IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public AccountBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<AccountDTO, AccountDM>();
                cfg.CreateMap<AccountDM, AccountDTO>();

                cfg.CreateMap<TB_AccountEntity, AccountDM>();
                cfg.CreateMap<AccountDM, TB_AccountEntity>();


                cfg.CreateMap<TB_AccountEntity, LoginDM>();
                cfg.CreateMap<LoginDM, TB_AccountEntity>();
                cfg.CreateMap<LoginDM, AccountDTO>().ReverseMap();
            });

            mapper = configuration.CreateMapper();
        }

        /// <summary>
        /// 取得分頁列表
        /// </summary>
        public PageResult<AccountDM> GetPageList(ListPageEntity pageEntity, AccountDM condition)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var dto = mapper.Map<AccountDTO>(condition);

            PageResult<AccountDTO> dtos = dao.FindPageList(pageEntity, dto, false);

            // 取得每個帳號的角色
            ITB_AccountSysRoleDAO roleDao = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
            ITB_SysRoleDAO sysRoleDao = _unitOfWork.Repository<ITB_SysRoleDAO>();

            foreach (var accountDto in dtos.Results)
            {
                var roleEntities = roleDao.FindListByPropertys(new Dictionary<string, object>
                {
                    { nameof(TB_AccountSysRoleEntity.AccountId), accountDto.Id }
                });

                accountDto.RoleNames = new List<string>();
                foreach (var roleEntity in roleEntities)
                {
                    var role = sysRoleDao.FindByPk(roleEntity.RoleId);
                    if (role != null && !string.IsNullOrEmpty(role.RoleName))
                    {
                        accountDto.RoleNames.Add(role.RoleName);
                    }
                }
            }

            PageResult<AccountDM> dms = new PageResult<AccountDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<AccountDM>>(dtos.Results)
            };

            return dms;
        }

        /// <summary>
        /// 取得單筆完整資訊
        /// </summary>
        public AccountDM GetInfo(Guid id)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByPk(id);
            if (entity == null) return null;

            var dm = mapper.Map<AccountDM>(entity);

            // 取得角色列表
            ITB_AccountSysRoleDAO roleDao = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
            var roleEntities = roleDao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(TB_AccountSysRoleEntity.AccountId), id }
            });

            dm.RoleIds = roleEntities.Select(r => r.RoleId.ToString()).ToList();

            return dm;
        }

        /// <summary>
        /// 檢查帳號是否存在
        /// </summary>
        public AccountDM CheckExist(string memberAccount)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entities = dao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(TB_AccountEntity.MemberAccount), memberAccount }
            });

            if (entities.Count > 0)
            {
                var entity = entities.Where(w => w.AccountStatus != AccountStatusEnum.Cancel.ToInt().ToString()).FirstOrDefault();
                if (entity == null) return null;

                return mapper.Map<AccountDM>(entity);
            }

            return null;
        }

        public Guid Create(AccountDM dm)
        {   
            using(var scope = new TransactionScope())
            {
                ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
                var newEntity = new TB_AccountEntity
                {
                    Id = Guid.NewGuid(),
                    MemberAccount = dm.MemberAccount,
                    AccountName = dm.AccountName,
                    AccountEmail = dm.AccountEmail,
                    AccountStatus = dm.AccountStatus ?? "1",
                    MemberPWD = Method_BL.EncryptionForPWD(dm.MemberAccount ?? "",dm.MemberPWD ?? ""),
                    LastLoginTime = DateTime.Now,
                    LastMemberPWDTime = DateTime.Now,
                    Logins = 0,

                };

                var entity = dao.Insert(newEntity);

                // 新增角色關聯
                if (dm.RoleIds != null && dm.RoleIds.Count > 0)
                {
                    ITB_AccountSysRoleDAO roleDao = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
                    foreach (var roleIdStr in dm.RoleIds)
                    {
                        if (Guid.TryParse(roleIdStr, out Guid roleId))
                        {
                            roleDao.InsertAction(new TB_AccountSysRoleEntity
                            {
                                Id = Guid.NewGuid(),
                                AccountId = newEntity.Id,
                                RoleId = roleId
                            });
                        }
                    }
                }

                dao.DbHelper.Commit();
                scope.Complete();
                return entity.Id;
            }
            

            
        }

        /// <summary>
        /// 編輯
        /// </summary>
        public void Edit(AccountDM dm)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByPk(dm.Id);

            if (entity == null)
            {
                throw new Exception("資料不存在");
            }

            entity.AccountName = dm.AccountName;
            entity.AccountEmail = dm.AccountEmail;
            entity.AccountStatus = dm.AccountStatus;

            entity.UpdatedAt = base.now;

            // 如果有新密碼才更新
            if (!string.IsNullOrEmpty(dm.MemberPWD))
            {
                entity.MemberPWD = Method_BL.EncryptionForPWD(dm.MemberAccount ?? "", dm.MemberPWD ?? "");
                entity.LastMemberPWDTime = base.now;
            }

            dao.Update(entity);

            // 更新角色關聯
            ITB_AccountSysRoleDAO roleDao = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
            var existingRoles = roleDao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(TB_AccountSysRoleEntity.AccountId), dm.Id }
            });

            // 刪除舊的角色關聯
            roleDao.Delete(existingRoles);

            // 新增新的角色關聯
            if (dm.RoleIds != null && dm.RoleIds.Count > 0)
            {
                foreach (var roleIdStr in dm.RoleIds)
                {
                    if (Guid.TryParse(roleIdStr, out Guid roleId))
                    {
                        roleDao.InsertAction(new TB_AccountSysRoleEntity
                        {
                            AccountId = dm.Id,
                            RoleId = roleId
                        });
                    }
                }
            }

            dao.DbHelper.Commit();
        }

        /// <summary>
        /// 刪除（軟刪除）
        /// </summary>
        public void Delete(Guid id)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByPk(id);

            if (entity != null)
            {
                entity.AccountStatus = AccountStatusEnum.Cancel.ToInt().ToString();
                dao.Update(entity);
                dao.DbHelper.Commit();
            }
        }

        /// <summary>
        /// 啟用/停用
        /// </summary>
        public void Enable(Guid id, int enable)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            dao.Enable(id, enable);
        }


        /// <summary>
        ///  pk 取得資料
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public AccountDM FindByPk(Guid accountId)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByPk(accountId);
            return mapper.Map<AccountDM>(entity);
        }

        public List<AccountDM> GetBackMemberList()
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var list = dao.FindListByPropertys(new Dictionary<string, object>()
            {
                {nameof(TB_AccountEntity.AccountStatus), AccountStatusEnum.Enabled.ToInt() },
            });

            return mapper.Map<List<AccountDM>>(list);
        }
    }
    //技師 Line
    public partial class AccountBL
    {
        /// <summary>
        /// 照服員 綁定Line
        /// </summary>
        /// <param name="userId"></param>
        public void LineAccountBind(string userId, Guid accountId)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();

            bool isExist = dao.IsExist(new Dictionary<string, object>()
            {
                //{nameof(TB_AccountEntity.LineUserId), userId },
                {nameof(TB_AccountEntity.AccountStatus), AccountStatusEnum.Enabled.ToInt() },
            }, accountId);

            if (isExist)
            {
                GetMessage().Add("error", "Line已綁定其他帳號");
                return;
            }

            var entity = dao.FindByPk(accountId);
            if (entity != null)
            {
                //entity.LineUserId = userId;
                dao.Update(entity);
                dao.DbHelper.Commit();
            }
        }
    }
}
