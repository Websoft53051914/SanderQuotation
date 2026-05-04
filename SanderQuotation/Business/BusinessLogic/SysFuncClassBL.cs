using AutoMapper;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.DB;
using Business.Common;
using Data.DataAccess.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DomainModel;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{

    public class SysFuncClassBL : BaseProjectBL
    {
        IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public SysFuncClassBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<SysFuncClassDTO, SysFuncClassDM>();
                cfg.CreateMap<SysFuncClassDM, SysFuncClassDTO>();

                cfg.CreateMap<TB_SysFuncClassEntity, SysFuncClassDM>();
                cfg.CreateMap<SysFuncClassDM, TB_SysFuncClassEntity>();

                cfg.CreateMap<TB_SysFuncClassEntity, SysFuncClassDM>();
                cfg.CreateMap<SysFuncClassDM, TB_SysFuncClassEntity>();
            });
            mapper = configuration.CreateMapper();
        }

        public PageResult<SysFuncClassDM> GetPageList(PageEntity pageEntity, SysFuncClassDM dm)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var dto = mapper.Map<SysFuncClassDTO>(dm);
            PageResult<SysFuncClassDTO> dtos = dao.FindPageList(pageEntity, dto);

            PageResult<SysFuncClassDM> dms = new PageResult<SysFuncClassDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<SysFuncClassDM>>(dtos.Results)
            };
            return dms;
        }

        public SysFuncClassDM GetInfo(Guid id)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = dao.FindByPk(id);
            var dm = mapper.Map<SysFuncClassDM>(entity);
            return dm;
        }

        public void Enable(Guid id, int enable)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = dao.FindByPk(id);
            entity.Status = enable;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public List<SysFuncClassDM> FindClassList()
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entities = dao.FindByAll();
            var dms = mapper.Map<List<SysFuncClassDM>>(entities);
            return dms;
        }

        public Guid Create(SysFuncClassDM dm)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = new TB_SysFuncClassEntity()
            {
                ClassName = dm.ClassName,
                Sequence = dm.Sequence,
                Memo = dm.Memo,
                Status = dm.Status,
            };
            dao.Insert(entity);
            dao.DbHelper.Commit();

            return entity.Id;
        }

        public void Edit(SysFuncClassDM dm)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = dao.FindByPk(dm.Id);
            entity.ClassName = dm.ClassName;
            entity.Sequence = dm.Sequence;
            entity.Memo = dm.Memo;
            entity.Status = dm.Status;
            entity.UpdatedAt = DateTime.Now;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public SysFuncClassDM CheckExist(string name)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = dao.FindByName(name);
            var dm = mapper.Map<SysFuncClassDM>(entity);
            return dm;
        }

        public void Delete(Guid id)
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entity = dao.FindByPk(id);
            entity.Status = 9;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        /// <summary>
        /// 取得所有功能類別
        /// </summary>
        public List<SysFuncClassDM> GetAll()
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entities = dao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(TB_SysFuncClassEntity.Status), 1 }
            });
            return mapper.Map<List<SysFuncClassDM>>(entities);
        }
    }
}
