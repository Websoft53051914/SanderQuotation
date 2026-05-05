namespace ViewModel.ImportTransferExcel
{
    /// <summary>
    /// 轉入檔案上傳 — 清單 Grid 顯示用 VM
    /// </summary>
    public class ImportTransferExcelGridVM
    {
        /// <summary>主鍵</summary>
        public long Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        public string BomFileName { get; set; } = "";

        /// <summary>匯入設定規則名稱</summary>
        public string ImportRuleName { get; set; } = "";

        /// <summary>客戶名稱</summary>
        public string CustomerName { get; set; } = "";

        /// <summary>產品料號</summary>
        public string ProductNo { get; set; } = "";

        /// <summary>客戶別（MM / D3 / STL / CTB / WT / AED / EDS）</summary>
        public string CustomerType { get; set; } = "";

        /// <summary>採購數量</summary>
        public int PurchaseQty { get; set; }

        /// <summary>狀態代碼（0=未轉檔、1=已轉檔、2=未查料、3=已查料、4=已查價）</summary>
        public int StatusCode { get; set; }

        /// <summary>狀態顯示文字</summary>
        public string Status { get; set; } = "";

        /// <summary>建立日期（顯示用）</summary>
        public string CreateTime { get; set; } = "";

        /// <summary>最後更新（顯示用）</summary>
        public string UpdateTime { get; set; } = "";
    }
}
