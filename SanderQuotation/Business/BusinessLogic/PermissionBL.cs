using AutoMapper;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.DB;
using Business.Common;
using Business.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dapper.SqlMapper;
using Data.DataAccess.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Dao;
using NPOI.SS.UserModel;
using static Const.Enums;
using Core.Utility.Utility;
using NPOI.OpenXmlFormats.Dml.Diagram;
using System.Transactions;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Text.RegularExpressions;
using Core.Utility.Extensions;


namespace Business.BusinessLogic
{
    public class PermissionBL : BaseProjectBL
    {
        IMapper mapper;
        IUnitOfWork _unitOfWork;
        public PermissionBL(IUnitOfWork unitOfWork)
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                //cfg.CreateMap<TB_SysRoleEntity, SysRoleDM>();
                //cfg.CreateMap<SysRoleDM, TB_SysRoleEntity>();

                cfg.CreateMap<SysRoleDTO, SysRoleDM>();
                cfg.CreateMap<SysRoleDM, SysRoleDTO>();

                //cfg.CreateMap<TB_SysFuncClassEntity, SysFuncClassDM>();
                //cfg.CreateMap<SysFuncClassDM, TB_SysFuncClassEntity>();

                cfg.CreateMap<AccountDTO, MemberDM>();
                cfg.CreateMap<MemberDM, AccountDTO>();

                cfg.CreateMap<TB_AccountEntity, MemberDM>();
                cfg.CreateMap<MemberDM, TB_AccountEntity>();

                cfg.CreateMap<TB_AccountEntity, AccountDM>();
                cfg.CreateMap<AccountDM, TB_AccountEntity>();
                 

            });

            mapper = configuration.CreateMapper();

            _unitOfWork = unitOfWork;
        }


        public PermissionBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
         
        public List<SysRoleDM> FindList(bool vmIsMaster = false)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            //var organization_Id = SessionVO.Organization_Id;
            //if (vmIsMaster)
            //    organization_Id = 0;
            var dtos = dao.FindList();
            //if (organization_Id == 0)
            //{
            //    foreach (var item in dtos)
            //    {
            //        item.RoleName = $"{item.Organization_Name}-{item.RoleName}";
            //    }
            //}
            var dms = mapper.Map<List<SysRoleDM>>(dtos);
            return dms;
        }


        public MemberDM GetAccountInfo(Guid id)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindDTOByPk(id);
            var dm = mapper.Map<MemberDM>(entity);

            

            return dm;
        }
         
       

        public void Delete(Guid id)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            using (TransactionScope scope = new())
            {
                var entity = dao.FindByPk(id);
                entity.AccountStatus = AccountStatusEnum.Cancel.ToInt().ToString();
                dao.Update(entity);
                 
                //dao.DbHelper.Commit();

                _unitOfWork.Commit();
                scope.Complete();
            }
               
        }

        public MemberDM CheckAccountNameExist(string accountName)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByAccountName(accountName);
            var dm = mapper.Map<MemberDM>(entity);
            return dm;
        }

        public void Enable(Guid id, int enable)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            dao.Enable(id, enable);
        }

        public void Create(MemberDM dm )
        {
            if (!Validate(dm))
            {
                return;
            }

            dm.MemberPWD = dm.TESTA;

            DateTime dtNow = DateTime.Now;
            if (string.IsNullOrEmpty(dm.MemberPWD))
                dm.MemberPWD = Guid.NewGuid().ToString();

            dm.MemberPWD = Method_BL.EncryptionForPWD(dm.MemberAccount, dm.MemberPWD);

            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>(); 
            using (TransactionScope scope = new())
            {
                var entity = dao.Insert(new TB_AccountEntity()
                {
                    AccountName = dm.AccountName,
                    AccountStatus = AccountStatusEnum.Enabled.ToInt().ToString(),
                    MemberAccount = dm.MemberAccount,
                    MemberPWD = dm.MemberPWD,
                    LastLoginTime = dtNow,
                    LastForgetPWDTime = dtNow,
                    LastMemberPWDTime = dtNow,
                    PermissionId = int.Parse(dm.PermissionIDs[0]),
                    LogoutTime = dtNow,
                    ResetPwdCode = dm.ResetPwdCode,

                    //EMPLOYEEBASICID = dm.EmployeeBasicId,
                    AccountEmail = dm.AccountEmail, 
                });

                ITB_AccountSysRoleDAO daoMR = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();

                foreach (var item in dm.PermissionIDs)
                {
                    daoMR.InsertAction(new TB_AccountSysRoleEntity()
                    {
                        AccountId = entity.Id,
                        RoleId = Guid.Parse(item),
                    });
                }
                 

                //dao.DbHelper.Commit();
                _unitOfWork.Commit();
                scope.Complete();
            }

        }

        public MemberDM CheckAccountExist(string memberAccount)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByAccount(memberAccount);
            var dm = mapper.Map<MemberDM>(entity);
            return dm;
        }

        public bool IsNumandEG(string word)
        {
            Regex NumandEG = new Regex("[^A-Za-z0-9]");
            return !NumandEG.IsMatch(word);
        }

        public bool Validate(MemberDM memberDM)
        {
            bool IsPass = true;

            #region 驗證資料
            MemberDM? ExistingAccount = CheckAccountExist(memberDM.MemberAccount);

            if (ExistingAccount != null && ExistingAccount.Id != memberDM.Id)
            {
                GetMessage().Add("Dupulicate", "帳號已存在系統中");
            }

            if (string.IsNullOrEmpty(memberDM.MemberAccount))
            {
                GetMessage().Add("Empty", "帳號不可為空值");
            }
            else
            {
                if (IsNumandEG(memberDM.MemberAccount) == false)
                {
                    GetMessage().Add("Empty", "帳號只能使用【英文、數字】");
                }
            }

            if (string.IsNullOrEmpty(memberDM.AccountName))
            {
                GetMessage().Add("Empty", "姓名不可為空值");
            }

            if (memberDM.PermissionIDs == null || memberDM.PermissionIDs.Count == 0)
            {
                GetMessage().Add("Empty", "請選擇角色");
            }          
            
            #endregion

            if (GetMessage().IsError())
            {
                IsPass = false;
            }

            return IsPass;


        }


        public void Edit(MemberDM dm)
        {
            //long newEmployeeBasicId = dm.EmployeeBasicId;
            if (!Validate(dm))
            {
                return;
            }

            dm.MemberPWD = dm.TESTA;

            DateTime dtNow = DateTime.Now;
            if (!string.IsNullOrEmpty(dm.MemberPWD))
                dm.MemberPWD = Method_BL.EncryptionForPWD(dm.MemberAccount, dm.MemberPWD);

            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>(); 
            using (TransactionScope scope = new())
            {
                var entity = dao.FindByPk(dm.Id);

                //long originEmployeeBasicId = entity.EmployeeBasicId;

                //entity.MemberAccount = dm.MemberAccount;
                entity.AccountName = dm.AccountName;

                if (!string.IsNullOrEmpty(dm.MemberPWD))
                    entity.MemberPWD = dm.MemberPWD;

                entity.AccountStatus = AccountStatusEnum.Enabled.ToInt().ToString();
                entity.UpdatedAt = DateTime.Now;

                entity.AccountEmail = dm.AccountEmail; 

                //entity.EmployeeBasicId = dm.EmployeeBasicId;

                ITB_AccountSysRoleDAO daoMR = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
                daoMR.DeleteByMemberId(dm.Id);

                foreach (var item in dm.PermissionIDs)
                {
                    daoMR.InsertAction(new TB_AccountSysRoleEntity()
                    {
                        AccountId = dm.Id,
                        RoleId = Guid.Parse(item),
                    });
                }
                 

                dao.Update(entity);
                _unitOfWork.Commit();
                scope.Complete();
            }
                
        }

        public List<Guid> FindRoles(Guid id)
        {
            ITB_AccountSysRoleDAO daoMR = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
            var entities = daoMR.FindListByMemberId(id);
            if (entities != null && entities.Count > 0)
            {
                return entities.Select(s => s.RoleId).ToList();
            }
            return new List<Guid>();
        }

        public void Unlock(Guid id)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var entity = dao.FindByPk(id);
            entity.Logins = 0;
            entity.LockTime = null;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }


        public PageResult<MemberDM> GetUnRolePageList(PageEntity pageEntity, MemberDM dm, bool isExpaied)
        {
            ITB_AccountDAO dao = _unitOfWork.Repository<ITB_AccountDAO>();
            var condition = mapper.Map<AccountDTO>(dm);

            PageResult<AccountDTO> dtos = dao.GetUnRolePageList(pageEntity, condition, isExpaied);

            ITB_AccountSysRoleDAO daoRole = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
            var allRoleData = daoRole.FindListByMemberIds(dtos.Results.Select(s => s.Id).ToList());

            //ISysRoleDAO daoRole = _unitOfWork.Repository<ISysRoleDAO>();
            //var group = allRoleData.GroupBy(g => g.RoleID);

            foreach (var item in dtos.Results)
            {
                var funcs = string.Join(",", allRoleData.Where(w => w.Id == item.Id).Select(s => s.RoleName));
                item.RoleName += $@"\r\n {funcs}";
            }

            PageResult<MemberDM> dms = new PageResult<MemberDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<MemberDM>>(dtos.Results)
            };

            return dms;
        }
    }
}
