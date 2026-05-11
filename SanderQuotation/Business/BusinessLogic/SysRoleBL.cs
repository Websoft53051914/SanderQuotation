using AutoMapper;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Business.Common;
using Business.DomainModel;
using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;
using Core.Utility.Extensions;

namespace Business.BusinessLogic
{
    public class SysRoleBL : BaseProjectBL
    {
        IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public SysRoleBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<SysRoleDTO, SysRoleDM>();
                cfg.CreateMap<SysRoleDM, SysRoleDTO>();

                cfg.CreateMap<TB_SysRoleEntity, SysRoleDM>();
                cfg.CreateMap<SysRoleDM, TB_SysRoleEntity>();

                cfg.CreateMap<PermissionDTO, PermissionDM>();
                cfg.CreateMap<PermissionDM, PermissionDTO>();


            });

            mapper = configuration.CreateMapper();
        }

        public List<PermissionDM> GetAllFuncList(Guid id)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var dtos = dao.GetAllFuncList(id);
            var dms = mapper.Map<List<PermissionDM>>(dtos);
            return dms;
        }

        public SysRoleDM CheckExist(string roleName)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entity = dao.FindByName(roleName);
            var dm = mapper.Map<SysRoleDM>(entity);
            return dm;
        }

        public Guid Create(SysRoleDM dm)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entity = new TB_SysRoleEntity()
            {
                RoleName = dm.RoleName,
                Memo = dm.Memo,
                Status = dm.Status, 
            };

            dao.Insert(entity);
            dao.DbHelper.Commit();

            return entity.Id;
        }

        public void Delete(Guid id)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entity = dao.FindByPk(id);
            entity.Status = 9;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public void Enable(Guid id, int enable)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            dao.Enable(id, enable);
        }

        public SysRoleDM GetInfo(Guid id)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entity = dao.GetInfo(id);
            var dm = mapper.Map<SysRoleDM>(entity);
            return dm;
        }

        public PageResult<SysRoleDM> GetPageList(PageEntity pageEntity, SysRoleDM dm)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var dto = mapper.Map<SysRoleDTO>(dm);

            PageResult<SysRoleDTO> dtos = dao.FindPageList(pageEntity, dto);

            ITB_SysFuncClassDAO funcClassDao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var SysFuncClassDtos = funcClassDao.FindList();

            PageResult<SysRoleDM> dms = new PageResult<SysRoleDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<SysRoleDM>>(dtos.Results)
            };

            return dms;
        }

        public void EditPermission(SysRoleDM dm)
        {
            ITB_SysRoleFuncDetailDAO dao = _unitOfWork.Repository<ITB_SysRoleFuncDetailDAO>();
            var entities = dao.FindListByRoleId(dm.Id);
            dao.Delete(entities);

            if (dm.PerCodes != null)
                foreach (var item in dm.PerCodes)
                {
                    dao.Insert(new TB_SysRoleFuncDetailEntity()
                    {
                        FuncDetailId = Guid.Parse(item),
                        RoleId = dm.Id
                    });
                }

            dao.DbHelper.Commit();
        }

        public void Edit(SysRoleDM dm)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entity = dao.FindByPk(dm.Id);
            entity.RoleName = dm.RoleName;
            entity.Memo = dm.Memo;
            entity.Status = dm.Status;
            entity.UpdatedAt = DateTime.Now;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public void RoleAddMember(Guid memberId, Guid roleId)
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var isExist = dao.IsExist(memberId, roleId);
            if (isExist == false)
            {
                ITB_AccountSysRoleDAO daoMR = _unitOfWork.Repository<ITB_AccountSysRoleDAO>();
                daoMR.Insert(new TB_AccountSysRoleEntity()
                {
                    AccountId = memberId,
                    RoleId = roleId,
                });
            }
        }

        /// <summary>
        /// 取得所有啟用的角色
        /// </summary>
        public List<SysRoleDM> GetAllRoles()
        {
            ITB_SysRoleDAO dao = _unitOfWork.Repository<ITB_SysRoleDAO>();
            var entities = dao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(TB_SysRoleEntity.Status), StatusEnum.Enabled.ToInt() }
            });
            return mapper.Map<List<SysRoleDM>>(entities);
        }

        /// <summary>
        /// 根據角色ID取得功能詳細ID清單
        /// </summary>
        public List<string> GetFuncDetailIdsByRole(Guid roleId)
        {
            ITB_SysRoleFuncDetailDAO dao = _unitOfWork.Repository<ITB_SysRoleFuncDetailDAO>();
            var entities = dao.FindListByRoleId(roleId);
            return entities.Select(e => e.FuncDetailId.ToString()).ToList();
        }

    }
}
