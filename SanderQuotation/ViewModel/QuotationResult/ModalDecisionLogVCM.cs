namespace ViewModel.QuotationResult
{
    /// <summary>
    /// 決策歷程 Modal 元件 VCM
    /// </summary>
    public class ModalDecisionLogVCM : BaseVCM
    {
        /// <summary>
        /// 取得決策歷程清單 API URL
        /// </summary>
        public string UrlGetDecisionLogs { get; set; } = string.Empty;

        /// <summary>
        /// 取得價格分群資料分頁列表 API URL
        /// </summary>
        public string UrlGetPriceClusterPageList { get; set; } = string.Empty;
    }
}
