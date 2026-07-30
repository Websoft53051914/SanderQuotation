using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    /// <summary>
    /// BOM 決策歷程
    /// </summary>
    public partial class TBBomFileDecisionLogBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBBomFileDecisionLogBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBBomFileDecisionLogEntity, TBBomFileDecisionLogDM>().ReverseMap();
                c.CreateMap<TBBomFileDecisionLogDTO, TBBomFileDecisionLogDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBBomFileDecisionLogBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBBomFileDecisionLogBL
    {
        #region -- TBBomFileDecisionLog --

        private ITBBomFileDecisionLogDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBBomFileDecisionLogDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBBomFileDecisionLogDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileDecisionLogDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBBomFileDecisionLogDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBBomFileDecisionLogDM> result = [];
            foreach (TBBomFileDecisionLogDTO item in dtoList)
            {
                TBBomFileDecisionLogDM dm = _mapper.Map<TBBomFileDecisionLogDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得啟用的資料清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBBomFileDecisionLogDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)Enums.StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBBomFileDecisionLogDM? GetOneInfo(Guid id)
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
        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 物理刪除指定 Status 的資料
        /// </summary>
        /// <param name="status">狀態值</param>
        public void PhysicalDeleteByStatus(int status)
        {
            GetDAO().PhysicalDeleteByStatus(status);
        }

        #endregion -- TBBomFileDecisionLog --
    }

    public partial class TBBomFileDecisionLogBL
    {
        /// <summary>
        /// 新增一筆決策歷程紀錄
        /// </summary>
        /// <param name="dm">決策歷程資料模型，Stage/Step/Message/BomFileContentId 須已填妥</param>
        public void DoInsert(TBBomFileDecisionLogDM dm)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            dm.Status = (int)StatusEnum.Enabled;
            dm.CreatedAt = nowTime;
            dm.CreatedBy = account;
            dm.UpdatedAt = nowTime;
            dm.UpdatedBy = account;

            TBBomFileDecisionLogEntity entity = _mapper.Map<TBBomFileDecisionLogEntity>(dm);
            GetDAO().InsertAction(entity);
            if (DoSaveChange)
                _unitOfWork.Commit();
        }

        /// <summary>
        /// 依 BomFileContentId 與 Stage 刪除決策歷程 (邏輯刪除)
        /// </summary>
        /// <param name="bomFileContentId">BOM 料項識別碼</param>
        /// <param name="stage">決策階段</param>
        public void DeleteByBomFileContentIdAndStage(Guid bomFileContentId, int stage)
        {
            SearchVO searchVO = new();
            searchVO.BomFileContentIdEq = bomFileContentId;
            searchVO.StageEq = stage;
            DeleteByFilter(searchVO);
        }

        /// <summary>
        /// 批次寫入 BomFileContentDM 中的暂存決策歷程。
        /// 當系統設定「AI 決策過程顯示開關」為關閉時不寫入。
        /// </summary>
        /// <param name="bomFileContentId">BOM 料項識別碼</param>
        /// <param name="logs">暂存決策歷程清單</param>
        public void DoInsertPendingLogs(Guid bomFileContentId, IEnumerable<TBBomFileDecisionLogDM> logs)
        {
            if (!IsAIDecisionProcessDisplayEnabled())
                return;

            List<TBBomFileDecisionLogDM> logList = logs.ToList();

            // 先刪除各階段的舊歷程
            foreach (int stage in logList.Where(l => l.Stage.HasValue).Select(l => l.Stage!.Value).Distinct())
            {
                DeleteByBomFileContentIdAndStage(bomFileContentId, stage);
            }

            foreach (TBBomFileDecisionLogDM log in logList)
            {
                log.BomFileContentId = bomFileContentId;
                DoInsert(log);
            }
        }

        private bool? _isAIDecisionProcessDisplayEnabled;

        /// <summary>
        /// 讀取 AI 決策過程顯示開關（無設定時預設開啟）；同一 BL 實例內快取結果。
        /// </summary>
        private bool IsAIDecisionProcessDisplayEnabled()
        {
            if (_isAIDecisionProcessDisplayEnabled.HasValue)
                return _isAIDecisionProcessDisplayEnabled.Value;

            TBSysSettingBL blTBSysSetting = new(_unitOfWork, SessionVO ?? new());
            blTBSysSetting._Configuration = _Configuration;
            List<TBSysSettingDM> list = blTBSysSetting.GetListByType(
                new SearchVO(),
                ParameterTypeEnum.AIDecisionProcessDisplaySwitch.ToString());
            TBSysSettingDM? dm = list.FirstOrDefault();
            _isAIDecisionProcessDisplayEnabled = dm == null
                || string.IsNullOrWhiteSpace(dm.Value)
                || dm.Value == "1";

            return _isAIDecisionProcessDisplayEnabled.Value;
        }
    }
}
