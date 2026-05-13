using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Microsoft.VisualBasic;
using static Const.Enums;

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

                cfg.CreateMap<ESDbTransferMappingDM, ESDbTransferMappingEntity>();
                cfg.CreateMap<ESDbTransferMappingEntity, ESDbTransferMappingDM>();

                cfg.CreateMap<EsFileTransferMappingColumnDM, EsFileTransferMappingColumnEntity>().ReverseMap();
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
            var entities = dao.FindListByProperty(nameof(ESDbTransferEntity.Status),StatusEnum.Enabled.ToInt());
            return mapper.Map<List<ESDbTransferDM>>(entities);
        }
    }

    public partial class ESDbTransferBL
    {
        public void CheckExist(ESDbTransferDM dm)
        {   
            if(dm.Id == Guid.Empty)
            {
                var entity = GetDAO().FindByPropertys(new Dictionary<string, object>() {
                    {nameof(ESDbTransferEntity.TransferCode),dm.TransferCode },
                    { nameof(ESDbTransferEntity.Status),StatusEnum.Enabled.ToInt()}
                });
                if (entity != null)
                {
                    GetMessage().SetAlert("此資料庫轉檔代碼已存在");
                }
            }
            
        }

        public PageResult<ESDbTransferDM> GetPageList(PageEntity pageEntity, SearchVO dm)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            IESDbTransferMappingDAO eSDbTransferMappingDAO = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            IEsFileTransferMappingColumnDAO esFileTransferMappingColumnDAO = _unitOfWork.Repository<IEsFileTransferMappingColumnDAO>();

            PageResult<ESDbTransferDTO> dtos = dao.FindPageList(pageEntity, dm);
            var dmList = mapper.Map<List<ESDbTransferDM>>(dtos.Results);
            var dbTransferMappingDMList = eSDbTransferMappingDAO.FindListByFilter(new SearchVO() { TransferCodeIn = dmList.Select(x => x.TransferCode).ToList() });
            var dbTransferMappingSrcDict = dbTransferMappingDMList.GroupBy(x => x.SrcDbTransferCode).ToDictionary(x => x.Key, x => x.Select(m => mapper.Map<ESDbTransferMappingDM>(m)).ToList());
            var dbTransferMappingDestDict = dbTransferMappingDMList.GroupBy(x => x.DstDbTransferCode).ToDictionary(x => x.Key, x => x.Select(m => mapper.Map<ESDbTransferMappingDM>(m)).ToList());

            var fileTransferMappingColumnDict = esFileTransferMappingColumnDAO.FindListByFilter(new SearchVO() { TransferCodeIn = dmList.Select(x => x.TransferCode).ToList() }).GroupBy(x => x.DBTransferMappingCode).ToDictionary(x => x.Key, x => x.Select(m => mapper.Map<EsFileTransferMappingColumnDM>(m)).ToList());

            for (int i=0;i< dmList.Count;i++)
            {
                var dmItem = dmList[i];
                if (dbTransferMappingSrcDict.ContainsKey(dmItem.TransferCode))
                {
                    dmItem.DbTransferMappingDMs = dbTransferMappingSrcDict[dmItem.TransferCode];
                }
                if (dbTransferMappingDestDict.ContainsKey(dmItem.TransferCode))
                {
                    if (dmItem.DbTransferMappingDMs.Count == 0)
                    {
                        dmItem.DbTransferMappingDMs = dbTransferMappingDestDict[dmItem.TransferCode];
                    }
                   
                }
                if (fileTransferMappingColumnDict.ContainsKey(dmItem.TransferCode))
                {
                    dmItem.FileTransferMappingColumnDMs = fileTransferMappingColumnDict[dmItem.TransferCode];
                }
            }

            PageResult<ESDbTransferDM> dms = new PageResult<ESDbTransferDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = dmList
            };

            return dms;
        }

        public Guid Create(ESDbTransferDM dm)
        {
            var entity = mapper.Map<ESDbTransferEntity>(dm);
            entity.Status = StatusEnum.Enabled.ToInt();
            var dtNow = DateTime.Now;
            entity.CreatedBy = UserInfo.UserAccount;
            entity.UpdatedBy = UserInfo.UserAccount;
            entity.CreatedAt = dtNow;
            entity.UpdatedAt = dtNow;

            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var result = dao.Insert(entity);

            return entity.Id;
        }

        public void Delete(List<Guid> ids)
        {
            IESDbTransferDAO dao = _unitOfWork.Repository<IESDbTransferDAO>();
            var entityList = dao.FindByPkList(ids);
            foreach (var entity in entityList)
            {
                entity.UpdatedAt = base.now;
                entity.UpdatedBy = UserInfo.UserAccount;
                entity.Status = StatusEnum.Cancel.ToInt();
                dao.Update(entity);
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
