using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class HistoryFileBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public HistoryFileBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.CreateMap<HistoryFileDM, HistoryFileEntity>().ReverseMap();
                c.CreateMap<HistoryFileDM, HistoryFileDTO>().ReverseMap();
                c.AllowNullCollections = true;
            });
            _mapper = cfg.CreateMapper();
        }

        public HistoryFileBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IHistoryFileDAO _historyFileDAO;
        private IHistoryFileDAO GetDAO()
        {
            _historyFileDAO ??= _unitOfWork.Repository<IHistoryFileDAO>();
            return _historyFileDAO;
        }

        private IEmbeddedHistoryFileDAO? embeddedHistoryFileDAO = null;
        private IEmbeddedHistoryFileDAO GetEmbeddedHistoryFileDAO()
        {
            embeddedHistoryFileDAO ??= _unitOfWork.Repository<IEmbeddedHistoryFileDAO>();
            return embeddedHistoryFileDAO;
        }


        /// <summary>
        /// 執行建立
        /// </summary>
        public void DoCreate(HistoryFileDM dmView)
        {
            using var scope = new TransactionScope();

            dmView.Status = (int)StatusEnum.Disabled;
            dmView.CreatedAt = dmView.UpdatedAt = base.now;
            dmView.UpdatedBy = dmView.CreatedBy = UserInfo?.UserAccount ?? "";
            HistoryFileEntity entity = _mapper.Map<HistoryFileEntity>(dmView);
            GetDAO().Insert(entity);
            dmView.Id = entity.Id;

            _unitOfWork.Commit();
            scope.Complete();
        }


        public HistoryFileDM? GetByUploadId(string uploadId)
        {
            var entity = GetDAO().FindByPropertys(new Dictionary<string, object>()
            {
                {nameof(HistoryFileEntity.UploadId), uploadId },
                {nameof(HistoryFileEntity.Status), (int)StatusEnum.Disabled },
            });
            if (entity == null)
                return null;
            return _mapper.Map<HistoryFileDM>(entity);
        }

        /// <summary>
        /// 執行刪除
        /// </summary>
        public void DoDelete(Guid id)
        {
            var entity = GetDAO().FindByPk(id);
            entity.UpdatedBy = UserInfo?.UserAccount ?? "";
            entity.UpdatedAt = base.now;
            entity.Status = (int)StatusEnum.Cancel;
            GetDAO().Update(entity);
            _unitOfWork.Commit();
        }


        public List<HistoryFileDM> GetListByFilter(SearchVO searchVO)
        {
            var entities = GetDAO().GetListByFilter(searchVO);
            return _mapper.Map<List<HistoryFileDM>>(entities);
        }

        public HistoryFileDM GetInfo(Guid id)
        {
            var entity = GetDAO().FindByPk(id);
            return _mapper.Map<HistoryFileDM>(entity);
        }

        /// <summary>
        /// 批次執行綁定
        /// </summary>
        /// <param name="dmView"></param>
        public void DoBindBatch(List<HistoryFileDM> dmListView)
        {
            var entityList = GetDAO().GetListByFilter(new SearchVO() { IdIn = dmListView.Select(x => x.Id).ToList()});
            foreach (HistoryFileEntity entity in entityList)
            {
                entity.UpdatedBy = UserInfo?.UserAccount ?? "";
                entity.UpdatedAt = base.now;
                entity.Status = (int)StatusEnum.Enabled;
                var dm = dmListView.Find(x => x.Id == entity.Id);
                entity.FileSummary = dm?.FileSummary;
                GetDAO().Update(entity);

                var embeddingEntity = GetEmbeddedHistoryFileDAO().FindByPropertys(new Dictionary<string, object>()
                {
                    {nameof(EmbeddedHistoryFileEntity.HistoryFileId), entity.Id },
                    {nameof(EmbeddedHistoryFileEntity.Status), (int)StatusEnum.Enabled },
                },new List<string>()
                {
                    nameof(EmbeddedHistoryFileEntity.Embedding)
                });
                if(embeddingEntity != null)
                {
                    embeddingEntity.UpdatedBy = UserInfo?.UserAccount ?? "";
                    embeddingEntity.UpdatedAt = base.now;
                    if(dm != null && dm.Embedding != null)
                    {
                        embeddingEntity.Embedding = dm.Embedding;
                    }
                    GetEmbeddedHistoryFileDAO().UpdateFile(embeddingEntity);
                }
                else
                {
                    EmbeddedHistoryFileEntity embeddedHistoryFileEntity = new EmbeddedHistoryFileEntity()
                    {
                        HistoryFileId = entity.Id,
                        Embedding = dm != null && dm.Embedding != null ? dm.Embedding : null,
                        CreatedAt = base.now,
                        CreatedBy = UserInfo?.UserAccount ?? "",
                        UpdatedAt = base.now,
                        UpdatedBy = UserInfo?.UserAccount ?? "",
                        Status = (int)StatusEnum.Enabled,
                    };
                    GetEmbeddedHistoryFileDAO().InsertFile(embeddedHistoryFileEntity);
                }
            }
            _unitOfWork.Commit();
        }

        public HistoryFileDM? GetById(Guid id)
        {
            var entity = GetDAO().FindByPk(id);
            if (entity == null) return null;
            return _mapper.Map<HistoryFileDM>(entity);
        }

        public PageResult<HistoryFileDM> GetPageList(PageEntity pageEntity, SearchVO dm)
        {

            PageResult<HistoryFileDTO> dtos = GetDAO().FindPageList(pageEntity, dm);
            var dmList = _mapper.Map<List<HistoryFileDM>>(dtos.Results);


      
            PageResult<HistoryFileDM> dms = new PageResult<HistoryFileDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = dmList
            };

            return dms;
        }



        public void Delete(List<Guid> guids)
        {
            var entityList = GetDAO().GetListByFilter(new SearchVO() { IdIn = guids});
            foreach (HistoryFileEntity entity in entityList)
            {
                entity.UpdatedBy = UserInfo?.UserAccount ?? "";
                entity.UpdatedAt = base.now;
                entity.Status = (int)StatusEnum.Cancel;
                GetDAO().Update(entity);
            }
            GetEmbeddedHistoryFileDAO().DeleteFile(guids,UserInfo?.UserAccount ?? "");
            _unitOfWork.Commit();
        }
    }
}
