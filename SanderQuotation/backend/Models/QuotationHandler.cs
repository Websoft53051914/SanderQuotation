using backend.AI;
using backend.Common;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using static Const.Enums;

namespace backend.Models
{
    /// <summary>
    /// 以 BomFileContent 為單位執行內部/外部查價的統一處理器
    /// </summary>
    public partial class QuotationHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OrderPriceDecison _orderPriceDecison;
        private readonly ExternalQuotationNexarHandler _externalQuotationNexarHandler;
        private readonly ExtractKeywordHandler _extractKeywordHandler;

        /// <summary>
        /// 是否執行內部查價
        /// </summary>
        public bool RunInternal { get; set; } = true;

        /// <summary>
        /// 是否執行外部查價
        /// </summary>
        public bool RunExternal { get; set; } = true;

        /// <summary>
        /// 是否寫入/刪除 TBBomFileDecisionLog 歷程紀錄（預設為 true）
        /// </summary>
        public bool WriteDecisionLog { get; set; } = true;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="scopeFactory">DI Scope 工廠</param>
        /// <param name="orderPriceDecison">內部採購價格 AI 決策器</param>
        /// <param name="externalQuotationNexarHandler">Nexar 外部查價處理器</param>
        /// <param name="extractKeywordHandler">AI 關鍵字抽取處理器（用於 Variant 客戶承認料抽取）</param>
        public QuotationHandler(
            IServiceScopeFactory scopeFactory,
            OrderPriceDecison orderPriceDecison,
            ExternalQuotationNexarHandler externalQuotationNexarHandler,
            ExtractKeywordHandler extractKeywordHandler)
        {
            _scopeFactory = scopeFactory;
            _orderPriceDecison = orderPriceDecison;
            _externalQuotationNexarHandler = externalQuotationNexarHandler;
            _extractKeywordHandler = extractKeywordHandler;
        }
    }

    public partial class QuotationHandler
    {
        /// <summary>
        /// 對單一 BOM 料項執行查價作業
        /// 依 RunInternal / RunExternal 屬性控制是否執行內部/外部查價，
        /// 並將決策過程記錄至 TBBomFileDecisionLog
        /// </summary>
        /// <param name="content">BOM 料項 DM，查價結果欄位將直接填入此物件</param>
        /// <param name="dmUpload">上傳檔案 DM，提供 CustomerCode 與 QuotationQty</param>
        /// <returns>已填入查價結果的 BomFileContentDM</returns>
        public async Task<BomFileContentDM> RunAsync(BomFileContentDM content, EsFileTransferUploadDM dmUpload)
        {
            TBBomFileDecisionLogBL? blLog = WriteDecisionLog ? BLFactory.GetInstanceBackGround<TBBomFileDecisionLogBL>() : null;

            string? customerCode = dmUpload.CustomerCode;
            int quotationQty = dmUpload.QuotationQty ?? 1;

            if (RunInternal)
                await RunInternalAsync(content, customerCode, blLog);

            if (RunExternal)
                await RunExternalAsync(content, quotationQty, blLog);

            return content;
        }

        /// <summary>
        /// 取得 Nexar 外部查價存取金鑰（執行外部查價前需先呼叫）
        /// </summary>
        public async Task AuthorizeExternalAsync()
        {
            await _externalQuotationNexarHandler.Authorization();
        }
    }

    public partial class QuotationHandler
    {
        /// <summary>
        /// 內部查價：以採購型號（No）查詢歷史採購紀錄，再以 AI 分析取得排單價
        /// </summary>
        /// <param name="content">BOM 料項 DM</param>
        /// <param name="customerCode">客戶代碼（來自上傳檔案），用於 Variant 客戶承認料過濾；空則不過濾</param>
        /// <param name="blLog">決策歷程 BL 實例</param>
        private async Task RunInternalAsync(BomFileContentDM content, string? customerCode, TBBomFileDecisionLogBL? blLog)
        {
            if (blLog != null)
            {
                SearchVO deleteLogSearchVO = new();
                deleteLogSearchVO.BomFileContentIdEq = content.Id;
                deleteLogSearchVO.StageEq = (int)BomFileDecisionLogStageEnum.InternalQuotation;
                blLog.DeleteByFilter(deleteLogSearchVO);
            }

            TBBomFileQuotationBL blTBBomFileQuotation = BLFactory.GetInstanceBackGround<TBBomFileQuotationBL>();

            SearchVO quotationSearchVO = new();
            quotationSearchVO.BomFileContentIdEq = content.Id;
            quotationSearchVO.IsLimit1 = true;

            TBBomFileQuotationDM? quotation = blTBBomFileQuotation.GetListByFilter(quotationSearchVO).FirstOrDefault();
            string? itemNo = quotation?.No;

            if (string.IsNullOrWhiteSpace(itemNo))
            {
                Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationResult, "查無採購型號（No），略過內部查價");
                return;
            }

            List<string>? approvedParts = await ResolveCustomerApprovedPartsAsync(itemNo, customerCode, content.Id, blLog);


            // 以 SearchVO 篩選：UnitCostLcy > 0，並依客戶承認料限縮 Description2
            SanderModulePurchaseLineBL blPurchaseLine = BLFactory.GetInstanceBackGround<SanderModulePurchaseLineBL>();
            SearchVO historySearchVO = new();
            historySearchVO.SanderModuleItemNoEq = itemNo;
            historySearchVO.UnitCostLcyGt = 0;
            if (approvedParts?.Count > 0)
                historySearchVO.Description2In = approvedParts;

            List<SanderModulePurchaseLineDM> history = blPurchaseLine.GetListByFilter(historySearchVO);

            InternalPurchaseRecordVO internalResult = await _orderPriceDecison.Search(history);

            if (!string.IsNullOrWhiteSpace(internalResult.Remark))
                Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationAIDecision, internalResult.Remark.Trim());

            string resultMsg;
            if (internalResult.UnitPriceTWD.HasValue)
                resultMsg = $"採用價格：{internalResult.UnitPriceTWD}（TWD），原幣：{internalResult.UnitPriceOriginalCurrency}（{internalResult.Currency}），採購日期：{internalResult.PurchaseOrderDate:yyyy-MM-dd}";
            else
                resultMsg = "查無可用歷史採購紀錄";

            Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationResult, resultMsg);

            content.InternalPurchaseOrderDate = internalResult.PurchaseOrderDate;
            content.InternalUnitPriceOriginalCurrency = internalResult.UnitPriceOriginalCurrency;
            content.InternalUnitPriceTwd = internalResult.UnitPriceTWD;
            content.InternalQuantity = internalResult.Quantity;
            content.InternalLowMinPrice = internalResult.InternalLowMinPrice;
            content.InternalLowMaxPrice = internalResult.InternalLowMaxPrice;
            content.InternalHighMinPrice = internalResult.InternalHighMinPrice;
            content.InternalHighMaxPrice = internalResult.InternalHighMaxPrice;
            content.IsFilterByCustomerApprovedPart = approvedParts != null && approvedParts.Count > 0;
            content.CustomerApprovedPartCsv = approvedParts != null && approvedParts.Count > 0
                ? string.Join(",", approvedParts)
                : null;
        }
    }

    public partial class QuotationHandler
    {
        /// <summary>
        /// 外部查價：以廠商型號（MPN）向 Nexar 查詢市場報價
        /// </summary>
        /// <param name="content">BOM 料項 DM</param>
        /// <param name="quotationQty">報價數量（來自上傳檔案），與 content.Qty 相乘後作為查詢數量</param>
        /// <param name="blLog">決策歷程 BL 實例</param>
        private async Task RunExternalAsync(BomFileContentDM content, int quotationQty, TBBomFileDecisionLogBL? blLog)
        {
            if (blLog != null)
            {
                SearchVO deleteLogSearchVO = new();
                deleteLogSearchVO.BomFileContentIdEq = content.Id;
                deleteLogSearchVO.StageEq = (int)BomFileDecisionLogStageEnum.ExternalQuotation;
                blLog.DeleteByFilter(deleteLogSearchVO);
            }

            string? mpn = content.ManufacturerPartNumber;

            if (string.IsNullOrWhiteSpace(mpn))
            {
                Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.ExternalQuotation, BomFileDecisionLogStepEnum.ExternalQuotationResult, "查無廠商型號（MPN），略過外部查價");
                return;
            }

            int qty = (content.Qty ?? 0) * quotationQty;
            Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.ExternalQuotation, BomFileDecisionLogStepEnum.ExternalQuotationNexarQuery, $"MPN：{mpn}，查詢數量：{qty}");

            List<string> preferredVendorList = GetPreferredVendorList();

            PriceBomVO bomVO = new();
            bomVO.ManufacturerPartNumber = mpn;
            bomVO.Qty = qty;

            ExternalQuotationRecordVO externalResult = await _externalQuotationNexarHandler.Search(bomVO, preferredVendorList);

            string resultMsg;
            if (externalResult.UnitPriceTWD.HasValue || externalResult.UnitPriceOriginalCurrency.HasValue)
            {
                resultMsg = $"查到報價：單價（TWD）= {externalResult.UnitPriceTWD}，原幣 = {externalResult.UnitPriceOriginalCurrency}，MOQ = {externalResult.MOQ}";
                content.ExternalQuotationDate = DateTime.Now;
                content.ExternalUnitPriceOriginalCurrency = externalResult.UnitPriceOriginalCurrency;
                content.ExternalUnitPriceTwd = externalResult.UnitPriceTWD;
                content.ExternalMoq = externalResult.MOQ;
                content.ExternalSupplierName = externalResult.SupplierName;
            }
            else
            {
                resultMsg = "查無外部報價";
            }

            Method.InsertDecisionLog(blLog, content.Id, BomFileDecisionLogStageEnum.ExternalQuotation, BomFileDecisionLogStepEnum.ExternalQuotationResult, resultMsg);
        }

        /// <summary>
        /// 取得優先供應商名稱清單（來自系統設定 PreferredVendorList）
        /// </summary>
        /// <returns>優先供應商名稱清單</returns>
        private List<string> GetPreferredVendorList()
        {
            TBSysSettingBL blTBSysSetting = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
            SearchVO sysSearchVO = new();
            return blTBSysSetting
                .GetListByType(sysSearchVO, ParameterTypeEnum.PreferredVendorList.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Value!)
                .ToList();
        }
    }

    public partial class QuotationHandler
    {
        /// <summary>
        /// 依據客戶代碼解析客戶承認料清單
        /// A. 有 CustomerCode：查 ReportItemCustomer → VariantCode → SanderModuleItemVariant
        ///    FlagNeedExtractKeyword=0 時直接使用 CustomerApprovedPartCsv；
        ///    非 0 時以 AI 從 Description/Description2 抽取 MPN
        /// B. 無 CustomerCode：回傳 null（不套用過濾）
        /// </summary>
        /// <param name="itemNo">TBBomFileQuotation.No（內部料號）</param>
        /// <param name="customerCode">EsFileTransferUpload.CustomerCode</param>
        /// <param name="contentId">BOM 料項 Id（用於寫決策日誌）</param>
        /// <param name="blLog">決策歷程 BL 實例</param>
        /// <returns>客戶承認料清單；null 表示不過濾</returns>
        private async Task<List<string>?> ResolveCustomerApprovedPartsAsync(
            string itemNo,
            string? customerCode,
            Guid contentId,
            TBBomFileDecisionLogBL? blLog)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
            {
                Method.InsertDecisionLog(blLog, contentId, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationVariantFilter, "無客戶代碼，不套用客戶承認料過濾");
                return null;
            }

            // Step1：以 CustomerCode 查 ReportItemCustomer，取得對應 VariantCode 清單
            ReportItemCustomerBL blReportItemCustomer = BLFactory.GetInstanceBackGround<ReportItemCustomerBL>();
            SearchVO vcSearchVO = new();
            vcSearchVO.CustomerCodeEq = customerCode;
            List<string> variantCodes = blReportItemCustomer
                .GetListByFilter(vcSearchVO)
                .Where(x => !string.IsNullOrWhiteSpace(x.VariantCode))
                .Select(x => x.VariantCode!)
                .Distinct()
                .ToList();

            if (variantCodes.Count == 0)
            {
                Method.InsertDecisionLog(blLog, contentId, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationVariantFilter, $"客戶代碼 {customerCode} 查無 Variant 設定，不套用過濾");
                return null;
            }

            // Step2：以 ItemNo + VariantCode 查 SanderModuleItemVariant，取得客戶承認料
            SanderModuleItemVariantBL blVariant = BLFactory.GetInstanceBackGround<SanderModuleItemVariantBL>();
            List<string> approvedParts = new();

            foreach (string variantCode in variantCodes)
            {
                SearchVO varSearchVO = new();
                varSearchVO.SanderModuleItemNoEq = itemNo;
                varSearchVO.CodeEq = variantCode;

                List<SanderModuleItemVariantDM> variants = blVariant.GetListByFilter(varSearchVO);

                foreach (SanderModuleItemVariantDM variant in variants)
                {
                    if (variant.FlagNeedExtractKeyword == 0)
                    {
                        if (!string.IsNullOrWhiteSpace(variant.CustomerApprovedPartCsv))
                        {
                            approvedParts.AddRange(variant.CustomerApprovedPartCsv
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(variant.Description) || !string.IsNullOrWhiteSpace(variant.Description2))
                        {
                            SanderModuleItemDM fakeItem = new();
                            fakeItem.No = variant.ItemNo;
                            fakeItem.LongDesc = variant.Description;
                            fakeItem.LongDesc2 = variant.Description2;

                            List<TBSanderModuleItemKeywordDM> keywords = await _extractKeywordHandler.ExtractKeyword([fakeItem]);
                            List<string> extractedForVariant = keywords
                                .Where(k => (k.ColumnName == "LongDesc" || k.ColumnName == "LongDesc2")
                                            && !string.IsNullOrWhiteSpace(k.Keyword))
                                .Select(k => k.Keyword!)
                                .ToList();
                            approvedParts.AddRange(extractedForVariant);

                            // 回寫 variant：FlagNeedExtractKeyword = 0，CustomerApprovedPartCsv = 本次抽取結果
                            string? csv = extractedForVariant.Count > 0 ? string.Join(",", extractedForVariant) : null;
                            blVariant.DoUpdateExtractResult(variant.Id, csv);
                        }
                    }
                }
            }

            if (approvedParts.Count > 0)
            {
                Method.InsertDecisionLog(blLog, contentId, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationVariantFilter,
                    $"套用客戶承認料過濾，客戶代碼：{customerCode}，Variant：{string.Join(", ", variantCodes)}，承認料清單（{approvedParts.Count} 筆）：{string.Join(", ", approvedParts)}");
                return approvedParts;
            }
            else
            {
                Method.InsertDecisionLog(blLog, contentId, BomFileDecisionLogStageEnum.InternalQuotation, BomFileDecisionLogStepEnum.InternalQuotationVariantFilter,
                    $"客戶代碼 {customerCode} 解析到 Variant 但承認料清單為空，不套用過濾");
                return null;
            }
        }
    }

    /// <summary>
    /// 供查價排程使用的 BOM 料項資訊
    /// </summary>
    public class PriceBomVO
    {
        /// <summary>
        /// 廠商型號（MPN）
        /// </summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>
        /// 需求數量
        /// </summary>
        public int? Qty { get; set; }
    }

    /// <summary>
    /// 內部採購紀錄
    /// </summary>
    public class InternalPurchaseRecordVO
    {
        /// <summary>
        /// 採購單日期
        /// </summary>
        public DateTime? PurchaseOrderDate { get; set; }

        /// <summary>
        /// 單價（原幣）
        /// </summary>
        public decimal? UnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 單價（台幣）
        /// </summary>
        public decimal? UnitPriceTWD { get; set; }

        /// <summary>
        /// 採購數量
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// 幣別代碼
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 供應商名稱
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// 歷史採購筆數
        /// </summary>
        public int HistoryCount { get; set; }

        /// <summary>
        /// 歷史採購 JSON 資料
        /// </summary>
        public string HistoryData { get; set; } = string.Empty;

        /// <summary>
        /// AI 分群決策低價群最低價
        /// </summary>
        public decimal? InternalLowMinPrice { get; set; }

        /// <summary>
        /// AI 分群決策低價群最高價
        /// </summary>
        public decimal? InternalLowMaxPrice { get; set; }

        /// <summary>
        /// AI 分群決策高價群最低價
        /// </summary>
        public decimal? InternalHighMinPrice { get; set; }

        /// <summary>
        /// AI 分群決策高價群最高價
        /// </summary>
        public decimal? InternalHighMaxPrice { get; set; }

        /// <summary>
        /// AI 決策備註
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 是否套用 Variant 客戶承認料過濾
        /// </summary>
        public bool IsFilterByCustomerApprovedPart { get; set; }
    }

    /// <summary>
    /// 外部查價紀錄
    /// </summary>
    public class ExternalQuotationRecordVO
    {
        /// <summary>
        /// 查價日期
        /// </summary>
        public DateTime? QuotationDate { get; set; }

        /// <summary>
        /// 單價（原幣）
        /// </summary>
        public decimal? UnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 單價（台幣）
        /// </summary>
        public decimal? UnitPriceTWD { get; set; }

        /// <summary>
        /// 最小訂購量（MOQ）
        /// </summary>
        public int? MOQ { get; set; }

        /// <summary>
        /// 幣別代碼
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 供應商名稱
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// 搜尋結果數量（全部供應商）
        /// </summary>
        public int SearchMatchCount { get; set; } = 0;

        /// <summary>
        /// 搜尋結果數量（優先供應商）
        /// </summary>
        public int SearchMatchPreferredCount { get; set; } = 0;
    }
}
