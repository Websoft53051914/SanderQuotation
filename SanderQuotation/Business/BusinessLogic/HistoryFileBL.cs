using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
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

        /// <summary>
        /// 批次執行綁定
        /// </summary>
        /// <param name="dmView"></param>
        public void DoBindBatch(List<HistoryFileDM> dmListView)
        {
            var entityList = GetDAO().GetListByFilter(new SearchVO() { IdIn = dmListView.Select(x => x.Id).ToList(),StatusEq = (int)StatusEnum.Disabled });
            foreach (HistoryFileEntity entity in entityList)
            {
                entity.UpdatedBy = UserInfo?.UserAccount ?? "";
                entity.UpdatedAt = base.now;
                entity.Status = (int)StatusEnum.Enabled;
                var dm = dmListView.Find(x => x.Id == entity.Id);
                entity.FileSummary = dm?.FileSummary;
                GetDAO().Update(entity);
            }
            _unitOfWork.Commit();
        }
    }
}
