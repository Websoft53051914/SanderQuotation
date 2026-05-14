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
    /// 系統設定
    /// </summary>
    public partial class TBSysSettingBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public TBSysSettingBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<TBSysSettingEntity, TBSysSettingDM>().ReverseMap();
                c.CreateMap<TBSysSettingDTO, TBSysSettingDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public TBSysSettingBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class TBSysSettingBL
    {
        #region -- TB_SysSetting --

        private ITBSysSettingDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public ITBSysSettingDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<ITBSysSettingDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<TBSysSettingDM> GetListByFilter(SearchVO searchVO)
        {
            List<TBSysSettingDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<TBSysSettingDM> result = [];
            foreach (TBSysSettingDTO item in dtoList)
            {
                TBSysSettingDM dm = _mapper.Map<TBSysSettingDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public TBSysSettingDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 查詢啟用狀態的清單
        /// </summary>
        /// <param name="searchVO"></param>
        /// <returns></returns>
        public List<TBSysSettingDM> GetListEnabled(SearchVO searchVO)
        {
            searchVO.StatusEq = (int)StatusEnum.Enabled;

            return GetListByFilter(searchVO);
        }

        #endregion

        /// <summary>
        /// 依 <see cref="TBSysSettingDM.Type"/> 查詢清單
        /// </summary>
        /// <param name="type">類型</param>
        /// <returns>DM 清單</returns>
        public List<TBSysSettingDM> GetListByType(SearchVO searchVO, string pType)
        {
            searchVO.TypeStrEq = pType;

            return GetListEnabled(searchVO);
        }

        /// <summary>
        /// 依 <see cref="TBSysSettingDM.Param"/> 查詢清單
        /// </summary>
        /// <param name="type">類型</param>
        /// <returns>DM 清單</returns>
        public List<TBSysSettingDM> GetListByParam(SearchVO searchVO, string pType, string pParam)
        {
            searchVO.ParamEq = pParam;

            return GetListByType(searchVO, pType);
        }
    }

    /// <summary>
    /// 內部系統設定
    /// </summary>
    public partial class TBSysSettingBL
    {
        /// <summary>
        /// 內部系統設定對應的 Type 值
        /// </summary>
        private const string TypeInternal = "Internal";
        /// <summary>
        /// 執行 ExtractKeyword 最後檢查時間
        /// </summary>
        public const string InternalParamLastCheckTimeForExtractKeyword = "LastCheckTimeForExtractKeyword";
        /// <summary>
        /// 內部系統設定預設值對照表，當資料庫中沒有對應設定時會使用此預設值
        /// </summary>
        private static readonly Dictionary<string, string> _dictDefaultValueInternal = new()
        {
            // 執行 ExtractKeyword 最後檢查時間
            { InternalParamLastCheckTimeForExtractKeyword, DateTime.MinValue.ToString("yyyy/MM/dd HH:mm:ss") }
        };

        public void DoSaveInternal(string key, string value)
        {
            string account = SessionVO?.Account ?? string.Empty;
            DateTime nowTime = DateTime.Now;

            SearchVO searchVO = new();
            searchVO.IsLimit1 = true;

            TBSysSettingDM? dm = GetListByParam(searchVO, TypeInternal, key).FirstOrDefault();
            if (dm != null)
            {
                // 已存在則更新
                dm.Value = value;
                dm.UpdatedBy = account;
                dm.UpdatedAt = nowTime;

                GetDAO().Update(_mapper.Map<TBSysSettingEntity>(dm));
                _unitOfWork.Commit();
            }
            else
            {
                // 不存在則新增
                dm = new TBSysSettingDM();
                dm.Type = TypeInternal;
                dm.Value = value;
                dm.Param = key;
                dm.Status = (int)StatusEnum.Enabled;
                dm.CreatedBy = account;
                dm.CreatedAt = nowTime;
                dm.UpdatedBy = account;
                dm.UpdatedAt = nowTime;

                GetDAO().Insert(_mapper.Map<TBSysSettingEntity>(dm));
            }
        }

        /// <summary>
        /// 取得設定值(string)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="defaultVal">預設值</param>
        /// <returns></returns>
        public string GetStringValueInternal(string key, string? defaultVal = null)
        {
            string defaultValConfig = _dictDefaultValueInternal.GetValueOrDefault(key, string.Empty);

            SearchVO searchVO = new();
            searchVO.IsLimit1 = true;

            TBSysSettingDM? dm = GetListByParam(searchVO, TypeInternal, key).FirstOrDefault();

            return dm?.Value ?? defaultVal ?? defaultValConfig ?? string.Empty;
        }

        /// <summary>
        /// 取得設定值(string)
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="defaultVal">預設值</param>
        /// <returns></returns>
        public DateTime GetDateTimeValueInternal(string key, DateTime? defaultVal = null)
        {
            string value = GetStringValueInternal(key, defaultVal?.ToString("yyyy/MM/dd HH:mm:ss"));

            if (DateTime.TryParse(value, out DateTime result))
            {
                return result;
            }

            return defaultVal ?? DateTime.MinValue;
        }
    }

    /**
     
     */
}
