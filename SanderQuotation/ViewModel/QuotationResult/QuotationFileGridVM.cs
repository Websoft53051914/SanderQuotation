using CommonClass.CustomAttribute;
using CommonClass.Model;

namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 定時查價結果 — 清單 Grid 顯示用 VM
    /// </summary>
    public class QuotationFileGridVM
    {
        /// <summary>流水號（顯示用）</summary>
        public int No { get; set; }

        /// <summary>主鍵（GUID）</summary>
        public Guid? Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        [Sort(IsDefault = false)]
        public string FileName { get; set; } = "";

        /// <summary>客戶代碼</summary>
        [Sort]
        public string? CustomerCode { get; set; }

        /// <summary>客戶名稱</summary>
        [Sort]
        public string? CustomerName { get; set; }

        /// <summary>
        /// 手動輸入客戶名稱（CustomerCode 為空時使用）
        /// </summary>
        public string? ManualCustomerName { get; set; }

        /// <summary>產品料號</summary>
        [Sort]
        public string? ProdNo { get; set; }

        /// <summary>報價數量</summary>
        [Sort]
        public int QuotationQty { get; set; }

        /// <summary>BOM 料項數</summary>
        [Sort]
        public int? ItemCount { get; set; }

        /// <summary>執行狀態碼</summary>
        [Sort]
        public int ProcessStatus { get; set; }

        /// <summary>執行狀態顯示文字</summary>
        public string ProcessStatusText { get; set; } = "";

        /// <summary>建立日期（顯示用）</summary>
        [Sort("CreatedAt", IsDefault = true, DefaultSortOrder = "DESC")]
        public string CreatedAtText { get; set; } = "";

        /// <summary>最後更新（顯示用）</summary>
        [Sort("UpdatedAt")]
        public string UpdatedAtText { get; set; } = "";
    }

    /// <summary>
    /// 定時查價結果清單搜尋 VM
    /// </summary>
    public class QuotationResultSearchVM : ListPageEntity
    {
        /// <summary>關鍵字篩選</summary>
        public string? KeywordLike { get; set; }
    }
}
