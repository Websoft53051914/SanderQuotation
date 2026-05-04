using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Microsoft.VisualBasic;

namespace Business.BusinessLogic
{
    public partial class ESDbTransferBL : BaseProjectBL
    {
        private IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public ESDbTransferBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

                cfg.CreateMap<ESDbTransferDM, ESDbTransferEntity>();
                cfg.CreateMap<ESDbTransferEntity, ESDbTransferDM>();

                cfg.CreateMap<ESDbTransferDM, ESDbTransferDTO>();
                cfg.CreateMap<ESDbTransferDTO, ESDbTransferDM>();
            });

            mapper = configuration.CreateMapper();
        }

        public ESDbTransferBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IESDbTransferDAO dao;
        private IESDbTransferDAO GetDAO()
        {
            dao ??= _unitOfWork.Repository<IESDbTransferDAO>();
            return dao;
        }

        public List<ESDbTransferDM> GetAll()
        {
            var dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var entities = dao.FindByAll();
            return mapper.Map<List<ESDbTransferDM>>(entities);
        }
    }

    public partial class ESDbTransferBL
    {
        public ESDbTransferDM GetByCode(string srcDbTransferCode)
        {
            var dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var entity = dao.FindByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferEntity.TransferCode), srcDbTransferCode }
            });
            return mapper.Map<ESDbTransferDM>(entity);
        }

        public PageResult<ESDbTransferDM> GetPageList(PageEntity pageEntity, ESDbTransferDM dm)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var dto = mapper.Map<ESDbTransferDTO>(dm);

            PageResult<ESDbTransferDTO> dtos = dao.FindPageList(pageEntity, dto);

            PageResult<ESDbTransferDM> dms = new PageResult<ESDbTransferDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<ESDbTransferDM>>(dtos.Results)
            };

            return dms;
        }

        public Guid Create(ESDbTransferDM dm)
        {
            var entity = mapper.Map<ESDbTransferEntity>(dm);
            entity.Status = 1;
            var dtNow = DateTime.Now;
            entity.CreatedBy = UserInfo.UserAccount;
            entity.UpdatedBy = UserInfo.UserAccount;
            entity.CreatedAt = dtNow;
            entity.UpdatedAt = dtNow;

            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var result = dao.Insert(entity);
            //dao.InsertAction(entity);
            dao.DbHelper.Commit();

            return entity.Id;
        }

        public void Delete(List<Guid> ids)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            foreach (var id in ids)
            {
                dao.Delete(id);
            }

            dao.DbHelper.Commit();
        }

        public ESDbTransferDM Get(Guid id)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var entity = dao.FindByPk(id);
            var newDM = mapper.Map<ESDbTransferDM>(entity);
            return newDM;
        }

        public void Edit(ESDbTransferDM dm)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var dto = dao.FindByPk(dm.Id);

            var entity = mapper.Map<ESDbTransferEntity>(dm);
            entity.Status = dto.Status;
            entity.Type = dto.Type;
            entity.SortNo = dto.SortNo;
            entity.Priority = dto.Priority;
            entity.CreatedAt = dto.CreatedAt;
            entity.CreatedBy = dto.CreatedBy;
            entity.UpdatedAt = DateTime.Now;

            dao.Update(entity);
            dao.DbHelper.Commit();
        }
    }
}
