using CommonClass.CustomAttribute;
using CommonClass.Model;

namespace ViewModel
{
    /// <summary>
    /// 轉入檔案上傳 VM（清單、上傳、編輯、刪除 共用）
    /// </summary>
    public class EsFileTransferUploadVM
    {
        // ── 清單顯示 ──

        /// <summary>
        /// 流水號（顯示用）
        /// </summary>
        public int No { get; set; }

        /// <summary>
        /// 主鍵（GUID）
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 原始檔案名稱
        /// </summary>
        [Sort(IsDefault = false)]
        public string FileName { get; set; } = "";

        /// <summary>
        /// 匯入規則代碼（EsFileTransferMapping.TransferMappingCode）
        /// </summary>
        [Sort]
        public string? TransferMappingCode { get; set; }

        /// <summary>
        /// 報價數量
        /// </summary>
        [Sort]
        public int QuotationQty { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        [Sort]
        public string? CustomerCode { get; set; }

        /// <summary>
        /// 客戶名稱（JOIN 取得）
        /// </summary>
        [Sort]
        public string? CustomerName { get; set; }

        /// <summary>
        /// BOM 料項數
        /// </summary>
        [Sort]
        public int? ItemCount { get; set; }

        /// <summary>
        /// 手動輸入客戶名稱（CustomerCode 為空時使用）
        /// </summary>
        public string? ManualCustomerName { get; set; }

        /// <summary>
        /// 執行狀態碼
        /// </summary>
        [Sort]
        public int ProcessStatus { get; set; }

        /// <summary>
        /// 執行狀態顯示文字
        /// </summary>
        public string ProcessStatusText { get; set; } = "";

        /// <summary>
        /// 建立日期（顯示用）
        /// </summary>
        [Sort("CreatedAt", IsDefault = true, DefaultSortOrder = "DESC")]
        public string CreatedAtText { get; set; } = "";

        /// <summary>
        /// 最後更新（顯示用）
        /// </summary>
        [Sort("UpdatedAt")]
        public string UpdatedAtText { get; set; } = "";

        // ── 搜尋/篩選 ──

        /// <summary>
        /// 關鍵字篩選（前端傳入）
        /// </summary>
        public string? Keyword { get; set; }

        // ── 上傳 Modal ──

        /// <summary>
        /// FilePond 上傳後回傳的 UploadId（tempGuid）
        /// </summary>
        public Guid? UploadId { get; set; }

        /// <summary>
        /// 匯入規則 FK（EsFileTransferMapping.Id，上傳時由前端傳入）
        /// </summary>
        public Guid? EsFileTransferMappingId { get; set; }
    }

    /// <summary>
    /// 轉入檔案上傳
    /// </summary>
    public class EsFileTransferUploadSearchVM : ListPageEntity
    {
        /// <summary>
        /// 關鍵字篩選（前端傳入）
        /// </summary>
        public string? KeywordLike { get; set; }
    }
}
