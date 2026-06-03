using ViewModel;
using ViewModel.QuotationResult;

namespace backend.Services.QuotationResult;

/// <summary>
/// 定時查價結果應用服務介面：清單、明細、重新查價、快查與決策/分群查詢。
/// </summary>
public interface IQuotationResultAppService
{
    /// <summary>功能說明：分頁取得定時查價結果（BOM 上傳檔）清單。</summary>
    /// <param name="filter">輸入參數：分頁、排序、KeywordLike（QuotationResultSearchVM）。</param>
    /// <returns>輸出參數：匿名物件 { Data, Total, Page, PageSize }。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：EsFileTransferUploadBL.GetPageListQuotationResult、GetProcessStatusText。
    /// 訊息內容及生成條件：由 Controller 轉 JsonSuccess；本層不產生錯誤字串。
    /// </remarks>
    object GetPageList(QuotationResultSearchVM filter);

    /// <summary>功能說明：依上傳檔主鍵取得查價明細（表頭 + 料項 + Mouser/DigiKey）。</summary>
    /// <param name="id">輸入參數：EsFileTransferUpload 主鍵 Guid。</param>
    /// <returns>輸出參數：QuotationFileEditVM；不存在時 null。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：EsFileTransferUploadBL.GetOneForEditQuotationResult、BomFileContentBL、TBBomFileQuotationOtherBL、TBSysSettingBL。
    /// 訊息內容及生成條件：null → Controller 回 JsonValidFail「資料不存在」。
    /// </remarks>
    QuotationFileEditVM? GetById(Guid id);

    /// <summary>功能說明：更新採購型號並重新執行內部查價，寫入單筆結果。</summary>
    /// <param name="request">輸入參數：BomFileContentId、No（QuotationReInternalQuotationRequestVM）。</param>
    /// <returns>輸出參數：QuotationResultServiceResult（Success / ErrorMessage）。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：TBBomFileQuotationBL.DoUpdateNo、QuotationHandler.RunInternalAsync、HandleQuotationBL.DoSaveSingleInternalQuotationResult。
    /// 訊息內容及生成條件：料項不存在 → Fail「資料不存在」；成功 → Ok()。
    /// </remarks>
    Task<QuotationResultServiceResult> ReInternalQuotationAsync(QuotationReInternalQuotationRequestVM request);

    /// <summary>功能說明：重新執行 Nexar 外部查價並寫入單筆結果。</summary>
    /// <param name="request">輸入參數：BomFileContentId（QuotationReExternalQuotationRequestVM）。</param>
    /// <returns>輸出參數：QuotationResultServiceResult。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：QuotationHandler.AuthorizeExternalAsync、RunExternalAsync、HandleQuotationBL.DoSaveSingleExternalQuotationResult。
    /// 訊息內容及生成條件：料項不存在 → Fail「資料不存在」；成功 → Ok()。
    /// </remarks>
    Task<QuotationResultServiceResult> ReExternalQuotationAsync(QuotationReExternalQuotationRequestVM request);

    /// <summary>功能說明：對單筆料號查 Mouser + DigiKey 現貨價並寫入 DB。</summary>
    /// <param name="request">輸入參數：BomFileContentId（QuotationCheckInStockPriceRequestVM）。</param>
    /// <returns>輸出參數：QuotationResultServiceResult。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：IExternalQueryExecuteHandler.QueryMouserAction、QueryDkAction、HandleQuotationBL.DoSaveSingleInStockPriceResult。
    /// 訊息內容及生成條件：料項不存在 → Fail「資料不存在」；成功 → Ok()。
    /// </remarks>
    Task<QuotationResultServiceResult> CheckInStockPriceAsync(QuotationCheckInStockPriceRequestVM request);

    /// <summary>功能說明：快查：查料 + 內部查價（不寫 DB），含決策歷程與價格分群預覽。</summary>
    /// <param name="request">輸入參數：料號、廠牌、客戶等（QuotationQuickSearchRequestVM）。</param>
    /// <returns>輸出參數：匿名物件 { Item, DecisionLogs, ClusterData }。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：QuotationHandler.RunPartSearchAsync、RunInternalAsync、SanderModulePurchaseLineBL。
    /// 訊息內容及生成條件：不持久化；由 Controller 包成 JsonSuccess。
    /// </remarks>
    Task<object> BuildQuickSearchResultAsync(QuotationQuickSearchRequestVM request);

    /// <summary>功能說明：取得啟用中的客戶下拉選項。</summary>
    /// <returns>輸出參數：List&lt;SelectItemVO&gt;（Text=客戶名稱, Value=客戶代碼）。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：ReportItemCustomerBL.GetListEnabled。
    /// 訊息內容及生成條件：無業務錯誤訊息；空清單仍回傳空 List。
    /// </remarks>
    List<SelectItemVO> GetCustomerList();

    /// <summary>功能說明：依 BOM 料項主鍵取得決策歷程清單。</summary>
    /// <param name="bomFileContentId">輸入參數：BomFileContent 主鍵 Guid。</param>
    /// <returns>輸出參數：List&lt;object&gt;（Stage、Step、Message 等匿名投影）。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：TBBomFileDecisionLogBL.GetListEnabled、BomFileDecisionLogStageEnum/StepEnum.GetDescription。
    /// 訊息內容及生成條件：無錯誤字串；無資料回傳空清單。
    /// </remarks>
    List<object> GetDecisionLogs(Guid bomFileContentId);

    /// <summary>功能說明：快查：僅查料比對（不寫 DB）。</summary>
    /// <param name="request">輸入參數：料號、廠牌、元件料號、描述（QuotationQuickPartMatchRequestVM）。</param>
    /// <returns>輸出參數：匿名物件 { No, MatchCategoryText, MatchField, DecisionLogs }。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：QuotationHandler.RunPartSearchAsync、GetBrandComparisonCategorySet。
    /// 訊息內容及生成條件：不持久化；由 Controller 包成 JsonSuccess。
    /// </remarks>
    Task<object> BuildQuickPartMatchResultAsync(QuotationQuickPartMatchRequestVM request);

    /// <summary>功能說明：快查：依勾選執行內部/外部/現貨查價（不寫 DB）。</summary>
    /// <param name="request">輸入參數：RunInternal、RunExternal、RunInStock 等（QuotationQuickPricingRequestVM）。</param>
    /// <returns>輸出參數：匿名物件 { Internal, External, InStock, ClusterMeta, DecisionLogs }。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：QuotationHandler、IExternalQueryExecuteHandler；各 Run* 旗標控制區塊。
    /// 訊息內容及生成條件：不持久化；未勾選的區塊對應屬性為 null。
    /// </remarks>
    Task<object> BuildQuickPricingResultAsync(QuotationQuickPricingRequestVM request);

    /// <summary>功能說明：價格分群採購紀錄分頁（可由料項 Id 或型號/區間參數查詢）。</summary>
    /// <param name="bomFileContentId">輸入參數：BomFileContent 主鍵（可 null，有值時優先從報價檔帶出 No 與區間）。</param>
    /// <param name="no">輸入參數：採購型號（bomFileContentId 為 null 時使用）。</param>
    /// <param name="customerApprovedPartCsv">輸入參數：客戶認可料號 CSV。</param>
    /// <param name="lowMinPrice">輸入參數：低價區間下限（手動模式）。</param>
    /// <param name="lowMaxPrice">輸入參數：低價區間上限。</param>
    /// <param name="highMinPrice">輸入參數：高價區間下限。</param>
    /// <param name="highMaxPrice">輸入參數：高價區間上限。</param>
    /// <param name="page">輸入參數：頁碼。</param>
    /// <param name="pageSize">輸入參數：每頁筆數。</param>
    /// <returns>輸出參數：{ ClusterRanges, Records, Total }；報價不存在時 Records 空、Total=0。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：TBBomFileQuotationBL.GetOneByBomFileContentId、SanderModulePurchaseLineBL.GetPageListPriceCluster。
    /// 訊息內容及生成條件：bomFileContentId 有值但無報價 → 空結果物件，非 Fail。
    /// </remarks>
    object GetPriceClusterPageList(
        Guid? bomFileContentId,
        string? no,
        string? customerApprovedPartCsv,
        decimal? lowMinPrice,
        decimal? lowMaxPrice,
        decimal? highMinPrice,
        decimal? highMaxPrice,
        int page,
        int pageSize);
}
