using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// 轉入檔案上傳資料
    /// </summary>
    public partial class EsFileTransferUploadBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public EsFileTransferUploadBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var cfg = new MapperConfiguration(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<EsFileTransferUploadEntity, EsFileTransferUploadDM>().ReverseMap();
                c.CreateMap<EsFileTransferUploadDTO, EsFileTransferUploadDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public EsFileTransferUploadBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

    }

    public partial class EsFileTransferUploadBL
    {
        #region -- EsFileTransferUpload --

        private IEsFileTransferUploadDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public IEsFileTransferUploadDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<IEsFileTransferUploadDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<EsFileTransferUploadDM> GetListByFilter(SearchVO searchVO)
        {
            List<EsFileTransferUploadDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<EsFileTransferUploadDM> result = [];
            foreach (EsFileTransferUploadDTO item in dtoList)
            {
                EsFileTransferUploadDM dm = _mapper.Map<EsFileTransferUploadDM>(item);

                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<EsFileTransferUploadDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public EsFileTransferUploadDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <summary>
        /// 刪除 status &lt;&gt; 1 且超過指定天數未更新的 EsFileTransferUpload 記錄及其實體檔案
        /// </summary>
        /// <param name="days">保留天數</param>
        /// <param name="fileDirectory">實體檔案所在目錄</param>
        public void DeleteOldNonActiveFiles(int days, string fileDirectory)
        {
            DateTime cutoff = DateTime.Now.AddDays(-days);
            var entityList = GetDAO().GetOldNonActiveList((int)StatusEnum.Enabled, cutoff);
            if (entityList.Count == 0) return;

            // 刪除實體檔案
            foreach (var entity in entityList)
            {
                try
                {
                    string ext = Path.GetExtension(entity.FileName);
                    string filePath = Path.Combine(fileDirectory, entity.UploadId + ext);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch { }
            }

            // 刪除資料庫記錄
            var ids = entityList.Select(x => x.Id).ToList();
            foreach (var id in ids)
                GetDAO().Delete(id);

            _unitOfWork.Commit();
        }

        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
            _unitOfWork.Commit();
        }

        #endregion -- EsFileTransferUpload --

        /// <summary>
        /// 依上傳 ID 取得單筆資料
        /// </summary>
        /// <param name="uploadId">上傳 ID</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public EsFileTransferUploadDM? GetOneInfoByUploadId(Guid uploadId)
        {
            SearchVO searchVO = new();
            searchVO.UploadIdEq = uploadId;
            searchVO.IsLimit1 = true;

            List<EsFileTransferUploadDM> list = GetListByFilter(searchVO);

            return list.FirstOrDefault();
        }
    }

    public partial class EsFileTransferUploadBL
    {
        /// <summary>
        /// 分頁查詢清單
        /// </summary>
        public PageResult<EsFileTransferUploadDM> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            PageResult<EsFileTransferUploadDTO> pageResult = GetDAO().GetPageList(pageEntity, searchVO);

            List<EsFileTransferUploadDM> results = [];
            foreach (EsFileTransferUploadDTO item in pageResult.Results)
            {
                EsFileTransferUploadDM dm = _mapper.Map<EsFileTransferUploadDM>(item);
                results.Add(dm);
            }

            return new PageResult<EsFileTransferUploadDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = results,
            };
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoCreateUpload(EsFileTransferUploadDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            EsFileTransferUploadEntity entity = _mapper.Map<EsFileTransferUploadEntity>(dm);
            entity.Status = (int)Enums.AccountStatusEnum.Disabled;
            entity.CreatedBy = account;
            entity.UpdatedBy = account;
            entity.CreatedAt = nowTime;
            entity.UpdatedAt = nowTime;
            entity.ProcessStatus = null;

            GetDAO().Insert(entity);
            dm.Id = entity.Id;
        }

        /// <summary>
        /// 更新資料-上傳
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoUpdateUpload(EsFileTransferUploadDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;

            EsFileTransferUploadEntity? entity = GetDAO().FindByPk(dm.Id);
            if (entity != null)
            {
                entity.UpdatedBy = account;
                entity.UpdatedAt = DateTime.Now;
                entity.EsFileTransferMappingId = dm.EsFileTransferMappingId;
                entity.QuotationQty = dm.QuotationQty;
                entity.CustomerCode = dm.CustomerCode;
                entity.ManualCustomerName = dm.ManualCustomerName;
                entity.ProdNo = dm.ProdNo;
                entity.ProcessStatus = (int)EsFileTransferUploadProcessStatusEnum.Pending;
                entity.Status = (int)Enums.StatusEnum.Enabled;

                GetDAO().Update(entity);
                _unitOfWork.Commit();
            }
        }

        /// <summary>
        /// 更新處理狀態
        /// </summary>
        /// <param name="id">檔案儲存代號</param>
        /// <param name="processStatus">目標處理狀態</param>
        public void DoUpdateProcessStatus(Guid id, int processStatus)
        {
            string account = SessionVO?.Account ?? string.Empty;

            EsFileTransferUploadEntity entity = GetDAO().FindByPk(id);
            if (entity != null)
            {
                entity.UpdatedBy = account;
                entity.UpdatedAt = DateTime.Now;
                entity.ProcessStatus = (int)processStatus;

                GetDAO().Update(entity);
                if (DoSaveChange)
                {
                    _unitOfWork.Commit();
                }
            }
        }

        /// <summary>
        /// 更新資料-編輯
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoUpdateEdit(EsFileTransferUploadDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;

            EsFileTransferUploadEntity? entity = GetDAO().FindByPk(dm.Id);
            if (entity != null)
            {
                entity.UpdatedBy = account;
                entity.UpdatedAt = DateTime.Now;
                entity.ProdNo = dm.ProdNo;

                GetDAO().Update(entity);
                _unitOfWork.Commit();
            }
        }
    }

    public partial class EsFileTransferUploadBL
    {

        /// <summary>
        /// 分頁查詢清單 - 定時查價結果
        /// </summary>
        public PageResult<EsFileTransferUploadDM> GetPageListQuotationResult(PageEntity pageEntity, SearchVO searchVO)
        {
            PageResult<EsFileTransferUploadDTO> pageResult = GetDAO().GetPageListQuotationResult(pageEntity, searchVO);

            List<EsFileTransferUploadDM> results = [];
            foreach (EsFileTransferUploadDTO item in pageResult.Results)
            {
                EsFileTransferUploadDM dm = _mapper.Map<EsFileTransferUploadDM>(item);
                results.Add(dm);
            }

            return new PageResult<EsFileTransferUploadDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = results,
            };
        }

        /// <summary>
        /// 查詢編輯資料
        /// </summary>
        public EsFileTransferUploadDM? GetOneForEditQuotationResult(Guid id, StatusEnum? statusEnum = StatusEnum.Enabled)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            if (statusEnum.HasValue)
            {
                searchVO.StatusEq = (int)statusEnum.Value;
            }

            PageEntity pageEntity = new();
            pageEntity.CurrentPage = 1;
            pageEntity.PageDataSize = 10;

            PageResult<EsFileTransferUploadDTO> pageResult = GetDAO().GetPageListQuotationResult(pageEntity, searchVO);

            if (pageResult.DataCount == 0)
            {
                return null;
            }

            EsFileTransferUploadDM dm = _mapper.Map<EsFileTransferUploadDM>(pageResult.Results[0]);

            return dm;
        }
    }

    public partial class EsFileTransferUploadBL
    {
        /// <summary>
        /// 刪除上傳紀錄及所有關聯資料（TBBomFileDecisionLog、BomFileContent、TBBomFileQuotation、TBBomFileQuotationExternalHistory）
        /// </summary>
        /// <param name="idList">EsFileTransferUpload ID 清單</param>
        public void DoDelete(List<Guid> idList)
        {
            BomFileContentBL blBomFileContent = GetBLBomFileContent();
            TBBomFileDecisionLogBL blDecisionLog = GetBLTBBomFileDecisionLog();
            TBBomFileQuotationBL blQuotation = GetBLTBBomFileQuotation();
            TBBomFileQuotationOtherBL blTBBomFileQuotationOther = GetBLTBBomFileQuotationOther();

            blBomFileContent.DoSaveChange = false;
            blDecisionLog.DoSaveChange = false;
            blQuotation.DoSaveChange = false;
            blTBBomFileQuotationOther.DoSaveChange = false;

            foreach (Guid id in idList)
            {
                EsFileTransferUploadDM? upload = GetOneInfo(id);
                if (upload?.UploadId == null) continue;

                SearchVO svBomContent = new();
                svBomContent.UploadIdEq = upload.UploadId;
                List<BomFileContentDM> contentList = blBomFileContent.GetListByFilter(svBomContent);

                foreach (BomFileContentDM content in contentList)
                {
                    SearchVO svByContentId = new();
                    svByContentId.BomFileContentIdEq = content.Id;

                    blDecisionLog.DeleteByFilter(svByContentId);
                    blQuotation.DeleteByFilter(svByContentId);
                    blTBBomFileQuotationOther.DeleteByFilter(svByContentId);
                }

                blBomFileContent.DeleteByFilter(svBomContent);
            }

            SearchVO svUpload = new();
            svUpload.IdIn = idList;
            DeleteByFilter(svUpload);

            blBomFileContent.DoSaveChange = true;
            blDecisionLog.DoSaveChange = true;
            blTBBomFileQuotationOther.DoSaveChange = true;
            blQuotation.DoSaveChange = true;        
        }
    }

    public partial class EsFileTransferUploadBL
    {
        private BomFileContentBL? _blBomFileContent = null;
        /// <summary>
        /// 取得 BomFileContentBL 實例
        /// </summary>
        protected BomFileContentBL GetBLBomFileContent()
        {
            _blBomFileContent ??= new BomFileContentBL(_unitOfWork, SessionVO ?? new());
            _blBomFileContent._Configuration = _Configuration;

            return _blBomFileContent;
        }

        private TBBomFileDecisionLogBL? _blTBBomFileDecisionLog = null;
        /// <summary>
        /// 取得 TBBomFileDecisionLogBL 實例
        /// </summary>
        protected TBBomFileDecisionLogBL GetBLTBBomFileDecisionLog()
        {
            _blTBBomFileDecisionLog ??= new TBBomFileDecisionLogBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileDecisionLog._Configuration = _Configuration;

            return _blTBBomFileDecisionLog;
        }

        private TBBomFileQuotationBL? _blTBBomFileQuotation = null;
        /// <summary>
        /// 取得 TBBomFileQuotationBL 實例
        /// </summary>
        protected TBBomFileQuotationBL GetBLTBBomFileQuotation()
        {
            _blTBBomFileQuotation ??= new TBBomFileQuotationBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotation._Configuration = _Configuration;

            return _blTBBomFileQuotation;
        }

        private TBBomFileQuotationOtherBL? _blTBBomFileQuotationOther = null;
        /// <summary>
        /// 取得 TBBomFileQuotationOtherBL 實例
        /// </summary>
        protected TBBomFileQuotationOtherBL GetBLTBBomFileQuotationOther()
        {
            _blTBBomFileQuotationOther ??= new TBBomFileQuotationOtherBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotationOther._Configuration = _Configuration;

            return _blTBBomFileQuotationOther;
        }

        private TBBomFileQuotationExternalHistoryBL? _blTBBomFileQuotationExternalHistory = null;
        /// <summary>
        /// 取得 TBBomFileQuotationExternalHistoryBL 實例
        /// </summary>
        protected TBBomFileQuotationExternalHistoryBL GetBLTBBomFileQuotationExternalHistory()
        {
            _blTBBomFileQuotationExternalHistory ??= new TBBomFileQuotationExternalHistoryBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotationExternalHistory._Configuration = _Configuration;

            return _blTBBomFileQuotationExternalHistory;
        }
    }
}
