using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// 處理查價相關邏輯
    /// </summary>
    public partial class HandleQuotationBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public HandleQuotationBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
            });
            _mapper = cfg.CreateMapper();
        }
    }

    /// <summary>
    /// 內部料品表 AI 關鍵字抽取
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 儲存料品關鍵字抽取結果：先刪除舊資料、新增關鍵字、更新 FlagNeedExtractKeyword 旗標，三步共用同一連線
        /// </summary>
        /// <param name="noList">料號清單（用於刪除舊關鍵字與更新旗標）</param>
        /// <param name="ids">資料 Id 清單（用於更新 FlagNeedExtractKeyword）</param>
        /// <param name="dmList">新關鍵字 DM 清單</param>
        public void DoSaveExtractKeyword(List<string> noList, List<Guid> ids, List<TBSanderModuleItemKeywordDM> dmList)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;
            GetBLTBSanderModuleItemKeyword().DoSaveChange = false;

            // 刪除舊關鍵字
            if (noList.Count > 0)
            {
                SearchVO deleteSearchVO = new();
                deleteSearchVO.SanderModuleItemNoIn = noList;

                GetBLTBSanderModuleItemKeyword().DeleteByFilter(deleteSearchVO);
            }

            // 新增關鍵字
            foreach (TBSanderModuleItemKeywordDM dm in dmList)
            {
                GetBLTBSanderModuleItemKeyword().DoInsert(dm);
            }

            // 更新 FlagNeedExtractKeyword
            if (ids.Count > 0)
            {
                GetBLSanderModuleItem().GetDAO().UpdateFlagNeedExtractKeyword(ids, false);
            }

            _unitOfWork.Commit();
            GetBLTBSanderModuleItemKeyword().DoSaveChange = true;
        }
    }

    /// <summary>
    /// 查料
    /// </summary>
    public partial class HandleQuotationBL
    {
        /// <summary>
        /// 批次儲存查料結果，並更新 EsFileTransferUpload.ProcessStatus = PartSearchDone，在同一 transaction 中完成
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload 主鍵</param>
        /// <param name="results">查料結果清單：BomFileContentId、採購型號、是否為建議料號</param>
        public void DoSavePartSearchResult(Guid uploadId, List<(Guid BomFileContentId, string? No, bool IsRecommendedNo)> results)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;
            GetBLTBBomFileQuotation().DoSaveChange = false;
            GetBLEsFileTransferUpload().DoSaveChange = false;

            foreach ((Guid contentId, string? no, bool isRecommendedNo) in results)
            {
                SearchVO searchVO = new();
                searchVO.BomFileContentIdEq = contentId;
                searchVO.IsLimit1 = true;

                TBBomFileQuotationDM? existing = GetBLTBBomFileQuotation().GetListByFilter(searchVO).FirstOrDefault();
                if (existing != null)
                {
                    TBBomFileQuotationEntity? entity = GetBLTBBomFileQuotation().GetDAO().FindByPk(existing.Id);
                    if (entity != null)
                    {
                        entity.No = no;
                        entity.IsRecommendedNo = isRecommendedNo;
                        entity.Status = (int)StatusEnum.Enabled;
                        entity.UpdatedBy = account;
                        entity.UpdatedAt = nowTime;
                        GetBLTBBomFileQuotation().GetDAO().Update(entity);
                    }
                }
                else
                {
                    TBBomFileQuotationEntity entity = new();
                    entity.No = no;
                    entity.IsRecommendedNo = isRecommendedNo;
                    entity.BomFileContentId = contentId;
                    entity.Status = (int)StatusEnum.Enabled;
                    entity.CreatedBy = account;
                    entity.UpdatedBy = account;
                    entity.CreatedAt = nowTime;
                    entity.UpdatedAt = nowTime;
                    GetBLTBBomFileQuotation().GetDAO().Insert(entity);
                }
            }

            GetBLEsFileTransferUpload().DoUpdateProcessStatus(uploadId, (int)EsFileTransferUploadProcessStatusEnum.PartSearchDone);

            _unitOfWork.Commit();
            GetBLTBBomFileQuotation().DoSaveChange = true;
            GetBLEsFileTransferUpload().DoSaveChange = true;
        }
    }

    public partial class HandleQuotationBL
    {
        private SanderModuleItemBL? _blSanderModuleItem = null;
        protected SanderModuleItemBL GetBLSanderModuleItem()
        {
            _blSanderModuleItem ??= new SanderModuleItemBL(_unitOfWork, SessionVO ?? new());
            _blSanderModuleItem._Configuration = _Configuration;

            return _blSanderModuleItem;
        }

        private BomFileContentBL? _blBomFileContent = null;
        protected BomFileContentBL GetBLBomFileContent()
        {
            _blBomFileContent ??= new BomFileContentBL(_unitOfWork, SessionVO ?? new());
            _blBomFileContent._Configuration = _Configuration;

            return _blBomFileContent;
        }

        private TBSanderModuleItemKeywordBL? _blTBSanderModuleItemKeyword = null;
        protected TBSanderModuleItemKeywordBL GetBLTBSanderModuleItemKeyword()
        {
            _blTBSanderModuleItemKeyword ??= new TBSanderModuleItemKeywordBL(_unitOfWork, SessionVO ?? new());
            _blTBSanderModuleItemKeyword._Configuration = _Configuration;

            return _blTBSanderModuleItemKeyword;
        }

        private TBBomFileQuotationBL? _blTBBomFileQuotation = null;
        protected TBBomFileQuotationBL GetBLTBBomFileQuotation()
        {
            _blTBBomFileQuotation ??= new TBBomFileQuotationBL(_unitOfWork, SessionVO ?? new());
            _blTBBomFileQuotation._Configuration = _Configuration;

            return _blTBBomFileQuotation;
        }

        private EsFileTransferUploadBL? _blEsFileTransferUpload = null;
        protected EsFileTransferUploadBL GetBLEsFileTransferUpload()
        {
            _blEsFileTransferUpload ??= new EsFileTransferUploadBL(_unitOfWork, SessionVO ?? new());
            _blEsFileTransferUpload._Configuration = _Configuration;

            return _blEsFileTransferUpload;
        }
    }
}
