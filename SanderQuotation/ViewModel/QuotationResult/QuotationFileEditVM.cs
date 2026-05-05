using System.Collections.Generic;

namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 定時查價結果 — 編輯頁 VM（包含 Header + BOM 料項明細）
    /// </summary>
    public class QuotationFileEditVM
    {
        /// <summary>主鍵</summary>
        public long Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        public string BomFileName { get; set; } = "";

        /// <summary>客戶名稱</summary>
        public string CustomerName { get; set; } = "";

        /// <summary>產品料號</summary>
        public string ProductNo { get; set; } = "";

        /// <summary>客戶別（MM / D3 / STL / CTB / WT / AED / EDS）</summary>
        public string CustomerType { get; set; } = "";

        /// <summary>採購數量（可修改）</summary>
        public int PurchaseQty { get; set; }

        /// <summary>建立日期（顯示用）</summary>
        public string CreateTime { get; set; } = "";

        /// <summary>最後更新（顯示用）</summary>
        public string UpdateTime { get; set; } = "";

        /// <summary>BOM 料項明細清單</summary>
        public List<QuotationItemVM> Items { get; set; } = new();
    }
}
