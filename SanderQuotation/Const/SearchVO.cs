namespace Const
{
    /// <summary>
    /// 篩選
    /// </summary>
    public partial class SearchVO
    {
        /// <summary>
        /// 是否只取單筆資料
        /// </summary>
        public bool IsLimit1 { get; set; } = false;
        /// <summary>
        /// 資料代號
        /// </summary>
        public Guid? IdEq { get; set; }
        /// <summary>
        /// 資料代號
        /// </summary>
        public Guid? IdNeq { get; set; }
        /// <summary>
        /// 資料代號
        /// </summary>
        public List<Guid> IdIn { get; set; } = [];
        /// <summary>
        /// 狀態
        /// </summary>
        public int? StatusEq { get; set; }
        /// <summary>
        /// 狀態
        /// </summary>
        public int? StatusNeq { get; set; }
        /// <summary>
        /// 名稱
        /// </summary>
        public string? NameEq { get; set; }
        /// <summary>
        /// 名稱
        /// </summary>
        public string? NameLike { get; set; }
        /// <summary>
        /// 主檔資料代號
        /// </summary>
        public int? MIdEq { get; set; }
        /// <summary>
        /// 主檔資料代號
        /// </summary>
        public List<int> MIdIn { get; set; } = [];
        /// <summary>
        /// 關鍵字
        /// </summary>
        public string? KeywordLike { get; set; }
        /// <summary>
        /// 關鍵字
        /// </summary>
        public List<string> KeywordLikeList { get; set; } = [];
        /// <summary>
        /// 檔案儲存代號
        /// </summary>
        public Guid? UploadIdEq { get; set; }
        /// <summary>
        /// 檔案儲存代號
        /// </summary>
        public List<string> UploadIdIn { get; set; } = [];
        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime? StartDateGte { get; set; }
        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime? StartDateLt { get; set; }
        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime? StartDateLte { get; set; }
        /// <summary>
        /// 結束日期
        /// </summary>
        public DateTime? EndDateGte { get; set; }
        /// <summary>
        /// 結束日期
        /// </summary>
        public DateTime? EndDateLte { get; set; }
        /// <summary>
        /// 代碼
        /// </summary>
        public string? CodeEq { get; set; }
        /// <summary>
        /// 類別
        /// </summary>
        public int? TypeEq { get; set; }

        /// <summary>
        /// 類型（字串）
        /// </summary>
        public string? TypeStrEq { get; set; }

        /// <summary>
        /// 參數名稱
        /// </summary>
        public string? ParamEq { get; set; }

        /// <summary>
        /// 查詢筆數上限（用於批次取資料）
        /// </summary>
        public int? LimitRows { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        public List<string> TypeStrIn { get; set; } = [];

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime? CreatedAtGte { get; set; }
    }

    public partial class SearchVO
    {
        /// <summary>
        /// 代碼
        /// </summary>
        public string? TransferMappingCodeEq { set; get; }

        /// <summary>
        /// 代碼
        /// </summary>
        public List<string> TransferMappingCodeIn { set; get; } = new List<string>();

        /// <summary>
        /// 代碼
        /// </summary>
        public string? ScheduleCycleCodeEq { set; get; }

        /// <summary>
        /// 代碼
        /// </summary>
        public List<string> ScheduleCycleCodeIn { set; get; } = new List<string>();
    }

    public partial class SearchVO
    {
        /// <summary>
        /// 資料庫轉檔代號
        /// </summary>
        public List<string> TransferCodeIn { get; set; } = new List<string>();

        /// <summary>
        /// SanderModuleItem 採購型號
        /// </summary>
        public string? SanderModuleItemNoEq { get; set; }

        /// <summary>
        /// SanderModuleItem 採購型號清單
        /// </summary>
        public List<string> SanderModuleItemNoIn { get; set; } = [];

        /// <summary>
        /// BomFileContent.Id
        /// </summary>
        public Guid? BomFileContentIdEq { get; set; }

        /// <summary>
        /// BomFileContent.Id 多值 IN 查詢
        /// </summary>
        public List<Guid> BomFileContentIdIn { get; set; } = [];

        /// <summary>
        /// TBSanderModuleItemKeyword 欄位名稱
        /// </summary>
        public string? SanderModuleItemKeywordColumnNameEq { get; set; }

        /// <summary>
        /// SanderModuleItem 是否需要執行 AI 關鍵字抽取
        /// </summary>
        public int? SanderModuleItemFlagNeedExtractKeywordEq { get; set; }

        /// <summary>
        /// 排除 description 標記為已停用／作廢的料品（用於 AI 關鍵字抽取）
        /// </summary>
        public bool ExcludeDeactivatedSanderModuleItem { get; set; }

        /// <summary>
        /// EsFileTransferUpload 執行狀態
        /// </summary>
        public int? ProcessStatusEq { get; set; }

        /// <summary>
        /// EsFileTransferUpload 對應的轉入規則 ID
        /// </summary>
        public Guid? EsFileTransferMappingIdEq { get; set; }

        /// <summary>
        /// 客戶代碼（用於 ReportItemCustomer 查詢）
        /// </summary>
        public string? CustomerCodeEq { get; set; }

        /// <summary>
        /// SanderModulePurchaseLine.Description2 清單（客戶承認料篩選）
        /// </summary>
        public List<string> Description2In { get; set; } = [];

        /// <summary>
        /// SanderModulePurchaseLine 本幣單價大於此值（用於過濾零元紀錄）
        /// </summary>
        public decimal? UnitCostLcyGt { get; set; }

        /// <summary>
        /// TBBomFileDecisionLog 決策階段
        /// </summary>
        public int? StageEq { get; set; }

        /// <summary>
        /// TBBomFileDecisionLog 決策階段
        /// </summary>
        public List<int> StageIn { get; set; } = [];

        /// <summary>
        /// 廠商型號
        /// </summary>
        public string? ManufacturerPartNumberEq { get; set; }

        /// <summary>
        /// 查價日期
        /// </summary>
        public DateTime? QuotationDateGte { get; set; }

        /// <summary>
        /// 查價日期
        /// </summary>
        public DateTime? QuotationDateLt { get; set; }

        /// <summary>
        /// BomFileContent.Id
        /// </summary>
        public Guid? BomFileContentIdNeq { get; set; }

        /// <summary>
        /// 內部查價日期
        /// </summary>
        public DateTime? InternalQuotationDateGte { get; set; }
    }

    /// <summary>
    /// 排序
    /// </summary>
    public partial class SearchVO
    {
        /// <summary>
        /// 排序欄位
        /// </summary>
        public List<string> OrderByColumnList { get; set; } = [];

        /// <summary>
        /// 內部查價日期
        /// </summary>
        public string? InternalQuotationDateOdr { get; set; } = "ASC";
    }
}
