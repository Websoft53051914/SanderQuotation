namespace Const
{
    public partial class SearchVO
    {
        /// <summary>
        /// 是否只取單筆資料
        /// </summary>
        public bool IsLimit1 { get; set; } = false;
        /// <summary>
        /// 資料代號
        /// </summary>
        public int? IdEq { get; set; }
        /// <summary>
        /// 資料代號
        /// </summary>
        public int? IdNeq { get; set; }
        /// <summary>
        /// 資料代號
        /// </summary>
        public List<int> IdIn { get; set; } = [];
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
        public string? UploadIdEq { get; set; }
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
    }

    public partial class SearchVO
    {
        /// <summary>
        /// 帳號狀態
        /// </summary>
        public string? AccountStatusEq { get; set; }

        /// <summary>
        /// 帳號狀態
        /// </summary>
        public string? AccountStatusNeq { get; set; }

        /// <summary>
        /// 帳號資料代號
        /// </summary>
        public int? AccountIdEq { get; set; }
    }

    public partial class SearchVO
    {
        /// <summary>
        /// 客戶姓名
        /// </summary>
        public string? CustomerNameLike { get; set; }
        /// <summary>
        /// 對話開始時間
        /// </summary>
        public DateTime? AILogStartTimeGte { get; set; }
        /// <summary>
        /// 對話開始時間
        /// </summary>
        public DateTime? AILogStartTimeLte { get; set; }
        /// <summary>
        /// 對應的 Enum
        /// </summary>
        public List<string> CategoryTypeIn { get; set; } = [];
        /// <summary>
        /// tb_aialert.Id
        /// </summary>
        public List<long> AIAlertIdIn { get; set; } = [];
        /// <summary>
        /// tb_aialert.Status
        /// </summary>
        public List<int> AIAlertStatusIn { get; set; } = [];
        /// <summary>
        /// tb_aiclose.Id
        /// </summary>
        public List<long> AICloseIdIn { get; set; } = [];
        /// <summary>
        /// tb_aiclose.Status
        /// </summary>
        public List<int> AICloseStatusIn { get; set; } = [];
    }

    public partial class SearchVO
    {
        /// <summary>
        /// 員工資料代號
        /// </summary>
        public int? EmployeeIdEq { get; set; }
        /// <summary>
        /// 員工資料代號
        /// </summary>
        public List<long> EmployeeIdIn { get; set; } = [];
        /// <summary>
        /// 員工資料代號
        /// </summary>
        public bool EmployeeIdIsNull { get; set; } = false;
        /// <summary>
        /// 差勤類型
        /// </summary>
        public int? AttendanceTypeEq { get; set; }
        /// <summary>
        /// 差勤類型
        /// </summary>
        public List<int> AttendanceTypeIn { get; set; } = [];
        /// <summary>
        /// 差勤子類型
        /// </summary>
        public int? AttendanceSubTypeEq { get; set; }

        public int? DeptIdEq { get; set; }

        /// <summary>
        /// 明細資料資料代號
        /// </summary>
        public int? DetailIdEq { get; set; }

        /// <summary>
        /// 統計日期
        /// </summary>
        public DateTime? ReportDateEq { get; set; }

        /// <summary>
        /// 年度
        /// </summary>
        public int? YearEq { get; set; }
    }

    public partial class SearchVO
    {
        /// <summary>
        /// TB_MessagePool.JobId
        /// </summary>
        public int? JobIdEq { get; set; }
        /// <summary>
        /// TB_MessagePool.SendStatus
        /// </summary>
        public int? SendStatusEq { get; set; }
    }
}
