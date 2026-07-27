using backend.AI;
using backend.Common;
using backend.Common.ConfigurationHelper;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Extensions;
using Data.DataAccess.DTO;
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
        private readonly ManualBatchEmbedding _manualBatchEmbedding;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 優先供應商快取（Lazy Loading；每次排程執行前設為 null 可清除快取）
        /// </summary>
        public List<string>? PreferredVendorList { get; set; }

        /// <summary>
        /// 需比對廠牌的料品類別快取（Lazy Loading；每次排程執行前設為 null 可清除快取）
        /// </summary>
        public HashSet<string>? BrandComparisonCategorySet { get; set; }

        /// <summary>
        /// 是否執行內部查價
        /// </summary>
        public bool RunInternal { get; set; } = true;

        /// <summary>
        /// 是否執行外部查價
        /// </summary>
        public bool RunExternal { get; set; } = true;

        /// <summary>
        /// 是否依效期快取嘗試辨識已有查價結果（true = 有效期內直接契用，false = 強制重新查價）
        /// </summary>
        public bool UseExpirationCache { get; set; } = true;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="scopeFactory">DI Scope 工廠</param>
        /// <param name="orderPriceDecison">內部採購價格 AI 決策器</param>
        /// <param name="externalQuotationNexarHandler">Nexar 外部查價處理器</param>
        /// <param name="extractKeywordHandler">AI 關鍵字抽取處理器（用於 Variant 客戶承認料抽取）</param>
        /// <param name="manualBatchEmbedding">向量化處理器（用於查料 Step3 Description 向量搜尋）</param>
        /// <param name="configuration">應用程式設定</param>
        public QuotationHandler(
            IServiceScopeFactory scopeFactory,
            OrderPriceDecison orderPriceDecison,
            ExternalQuotationNexarHandler externalQuotationNexarHandler,
            ExtractKeywordHandler extractKeywordHandler,
            ManualBatchEmbedding manualBatchEmbedding,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _orderPriceDecison = orderPriceDecison;
            _externalQuotationNexarHandler = externalQuotationNexarHandler;
            _extractKeywordHandler = extractKeywordHandler;
            _manualBatchEmbedding = manualBatchEmbedding;
            _configuration = configuration;
        }
    }

    public partial class QuotationHandler
    {
        /// <summary>
        /// 依 tb_syssetting 設定自動查價是否執行內部／外部查價（無設定時預設皆執行）
        /// </summary>
        public void ApplyPricingSearchSettingsFromSysSetting()
        {
            TBSysSettingBL blTBSysSetting = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
            List<TBSysSettingDM> list = blTBSysSetting.GetListEnabled(new SearchVO());

            RunInternal = GetPricingSearchSwitch(list, ParameterTypeEnum.PricingSearchRunInternalSwitch, true);
            RunExternal = GetPricingSearchSwitch(list, ParameterTypeEnum.PricingSearchRunExternalSwitch, true);
        }

        private static bool GetPricingSearchSwitch(List<TBSysSettingDM> list, ParameterTypeEnum type, bool defaultValue)
        {
            TBSysSettingDM? dm = list.FirstOrDefault(x => x.Type == type.ToString());
            if (dm == null || string.IsNullOrWhiteSpace(dm.Value))
                return defaultValue;

            return dm.Value == "1";
        }

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
            string? customerCode = dmUpload.CustomerCode;
            int quotationQty = dmUpload.QuotationQty ?? 1;

            await RunPartSearchAsync(content, GetBrandComparisonCategorySet());

            if (RunInternal)
                await RunInternalAsync(content, customerCode);

            if (RunExternal)
                await RunExternalAsync(content, quotationQty);

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
        /// 若料項為建議料號（IsRecommendedNo = true）則略過，不執行內部查價
        /// </summary>
        /// <param name="content">BOM 料項 DM</param>
        /// <param name="customerCode">客戶代碼（來自上傳檔案），用於 Variant 客戶承認料過濾；空則不過濾</param>
        public async Task RunInternalAsync(BomFileContentDM content, string? customerCode)
        {
            content.InternalPurchaseOrderDate = null;
            content.InternalUnitPriceOriginalCurrency = null;
            content.InternalUnitPriceTwd = null;
            content.InternalQuantity = null;
            content.InternalCurrency = null;
            content.InternalSupplierName = null;
            content.InternalSupplierCode = null;
            content.InternalItemDescription2 = null;
            content.InternalLowMinPrice = null;
            content.InternalLowMaxPrice = null;
            content.InternalHighMinPrice = null;
            content.InternalHighMaxPrice = null;
            content.IsFilterByCustomerApprovedPart = false;
            content.CustomerApprovedPartCsv = null;

            string? itemNo = content.No;

            if (string.IsNullOrWhiteSpace(itemNo))
            {
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationResult, "查無採購型號（No），略過內部查價");
                return;
            }
            else if (content.IsRecommendedNo)
            {
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationResult, "採購型號為建議料號，略過內部查價");
                return;
            }

            if (UseExpirationCache)
            {
                ConfigurationHelper configHelper = new(_configuration);
                int internalExpirationDays = configHelper.GetIntValue("InternalQuotation:ExpirationDay");

                TBBomFileQuotationBL blTBBomFileQuotation = BLFactory.GetInstanceBackGround<TBBomFileQuotationBL>();
                TBBomFileQuotationDM? cachedQuotation = blTBBomFileQuotation.GetOneForExpirationCache(itemNo, internalExpirationDays, content.Id, customerCode);

                if (cachedQuotation != null && cachedQuotation.InternalUnitPriceTwd.HasValue)
                {
                    content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationResult,
                        $"[效期快取] 採用 {internalExpirationDays} 天內紀錄，單價（TWD）= {cachedQuotation.InternalUnitPriceTwd}，查價時間：{cachedQuotation.InternalQuotationDate:yyyy-MM-dd HH:mm}");

                    content.InternalPurchaseOrderDate = cachedQuotation.InternalPurchaseOrderDate;
                    content.InternalUnitPriceOriginalCurrency = cachedQuotation.InternalUnitPriceOriginalCurrency;
                    content.InternalUnitPriceTwd = cachedQuotation.InternalUnitPriceTwd;
                    content.InternalQuantity = cachedQuotation.InternalQuantity;
                    content.InternalCurrency = cachedQuotation.InternalCurrency;
                    content.InternalSupplierName = cachedQuotation.InternalSupplierName;
                    content.InternalSupplierCode = cachedQuotation.InternalSupplierCode;
                    content.InternalItemDescription2 = cachedQuotation.InternalItemDescription2;
                    content.InternalLowMinPrice = cachedQuotation.InternalLowMinPrice;
                    content.InternalLowMaxPrice = cachedQuotation.InternalLowMaxPrice;
                    content.InternalHighMinPrice = cachedQuotation.InternalHighMinPrice;
                    content.InternalHighMaxPrice = cachedQuotation.InternalHighMaxPrice;
                    content.IsFilterByCustomerApprovedPart = cachedQuotation.IsFilterByCustomerApprovedPart;
                    content.CustomerApprovedPartCsv = cachedQuotation.CustomerApprovedPartCsv;
                    return;
                }
            }

            List<string>? approvedParts = await ResolveCustomerApprovedPartsAsync(itemNo, customerCode, content);

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
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationAIDecision, internalResult.Remark.Trim());

            string resultMsg;
            if (internalResult.UnitPriceTWD.HasValue)
                resultMsg = $"採用價格：{internalResult.UnitPriceTWD}（TWD），原幣：{internalResult.UnitPriceOriginalCurrency}（{internalResult.Currency}），採購日期：{internalResult.PurchaseOrderDate:yyyy-MM-dd}";
            else
                resultMsg = "查無可用歷史採購紀錄";

            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationResult, resultMsg);

            content.InternalPurchaseOrderDate = internalResult.PurchaseOrderDate;
            content.InternalUnitPriceOriginalCurrency = internalResult.UnitPriceOriginalCurrency;
            content.InternalUnitPriceTwd = internalResult.UnitPriceTWD;
            content.InternalQuantity = internalResult.Quantity;
            content.InternalCurrency = internalResult.Currency;
            content.InternalSupplierName = internalResult.SupplierName;
            content.InternalSupplierCode = internalResult.SupplierCode;
            content.InternalItemDescription2 = internalResult.Description2;
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
        public async Task RunExternalAsync(BomFileContentDM content, int quotationQty)
        {
            content.ExternalQuotationDate = null;
            content.ExternalUnitPriceOriginalCurrency = null;
            content.ExternalUnitPriceTwd = null;
            content.ExternalMoq = null;
            content.ExternalCurrency = null;
            content.ExternalSupplierName = null;
            content.ExternalStock = null;
            content.ExternalScenario = null;

            string? mpn = content.ManufacturerPartNumber;

            if (string.IsNullOrWhiteSpace(mpn))
            {
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.ExternalQuotation, (int)BomFileDecisionLogStepEnum.ExternalQuotationResult, "查無廠商型號（MPN），略過外部查價");
                return;
            }

            // BOM 數量允許小數；外部 MOQ・庫存以 decimal 直接比對
            // 僅 0 < demandQty < 1 時進成 1（例如 0.006 → 1）；其餘保留小數
            decimal demandQty = (content.Qty ?? 0m) * quotationQty;
            decimal qty = demandQty <= 0 ? 0m
                : demandQty < 1 ? 1m
                : demandQty;
            List<string> preferredVendorList = GetPreferredVendorList();

            if (UseExpirationCache)
            {
                ConfigurationHelper configHelper = new(_configuration);
                int externalExpirationDays = configHelper.GetIntValue("ExternalQuotation:ExpirationDay");

                TBBomFileQuotationExternalHistoryBL blTBBomFileQuotationExternalHistory = BLFactory.GetInstanceBackGround<TBBomFileQuotationExternalHistoryBL>();
                List<TBBomFileQuotationExternalHistoryDM> historyList = blTBBomFileQuotationExternalHistory.GetListForExpirationCache(mpn, externalExpirationDays);

                List<TBBomFileQuotationExternalHistoryDM> filteredList = historyList
                    .Where(h => (h.Moq ?? 0) <= qty && (h.Stock ?? 0) >= qty && (h.UnitPriceTwd.HasValue || h.UnitPriceOriginalCurrency.HasValue))
                    .GroupBy(h => h.SupplierName ?? string.Empty)
                    .Select(g => g.OrderByDescending(h => h.Moq ?? 0).ThenBy(h => h.UnitPriceTwd ?? decimal.MaxValue).First())
                    .ToList();

                if (filteredList.Count > 0)
                {
                    List<TBBomFileQuotationExternalHistoryDM> preferredList = filteredList
                        .Where(h => preferredVendorList.Contains(h.SupplierName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    TBBomFileQuotationExternalHistoryDM selected = preferredList.Count > 0
                        ? preferredList.OrderBy(h => h.UnitPriceTwd ?? decimal.MaxValue).First()
                        : filteredList.OrderBy(h => h.UnitPriceTwd ?? decimal.MaxValue).First();

                    bool isPreferred = preferredList.Count > 0;

                    content.AddDecisionLog((int)BomFileDecisionLogStageEnum.ExternalQuotation, (int)BomFileDecisionLogStepEnum.ExternalQuotationResult,
                        $"[效期快取] 採用 {externalExpirationDays} 天內外部查價紀錄，MPN：{mpn}，供應商：{selected.SupplierName}，單價（TWD）= {selected.UnitPriceTwd}，查價日期：{selected.QuotationDate:yyyy-MM-dd}，情境：{(isPreferred ? ExternalScenarioEnum.PreferredVendor.GetDescription() : ExternalScenarioEnum.Fallback.GetDescription())}");

                    content.ExternalQuotationDate = selected.QuotationDate;
                    content.ExternalUnitPriceOriginalCurrency = selected.UnitPriceOriginalCurrency;
                    content.ExternalUnitPriceTwd = selected.UnitPriceTwd;
                    content.ExternalMoq = selected.Moq;
                    content.ExternalCurrency = selected.Currency;
                    content.ExternalSupplierName = selected.SupplierName;
                    content.ExternalStock = selected.Stock;
                    content.ExternalScenario = isPreferred
                        ? (int)ExternalScenarioEnum.PreferredVendor
                        : (int)ExternalScenarioEnum.Fallback;
                    return;
                }
            }

            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.ExternalQuotation, (int)BomFileDecisionLogStepEnum.ExternalQuotationNexarQuery, $"MPN：{mpn}，查詢數量：{qty}");

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
                content.ExternalCurrency = externalResult.Currency;
                content.ExternalSupplierName = externalResult.SupplierName;
                content.ExternalStock = externalResult.Stock;
                content.ExternalScenario = externalResult.IsPreferred
                    ? (int)ExternalScenarioEnum.PreferredVendor
                    : (int)ExternalScenarioEnum.Fallback;
            }
            else if (!string.IsNullOrWhiteSpace(externalResult.ErrorMessage))
            {
                resultMsg = $"查詢失敗，錯誤訊息：{externalResult.ErrorMessage}";
            }
            else
            {
                resultMsg = "查無外部報價";
            }

            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.ExternalQuotation, (int)BomFileDecisionLogStepEnum.ExternalQuotationResult, resultMsg);
        }

        /// <summary>
        /// 取得優先供應商名稱清單（來自系統設定 PreferredVendorList，Lazy Loading）
        /// </summary>
        /// <returns>優先供應商名稱清單</returns>
        private List<string> GetPreferredVendorList()
        {
            if (PreferredVendorList != null)
                return PreferredVendorList;

            TBSysSettingBL blTBSysSetting = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
            SearchVO sysSearchVO = new();
            PreferredVendorList = blTBSysSetting
                .GetListByType(sysSearchVO, ParameterTypeEnum.PreferredVendorList.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Value!)
                .ToList();
            return PreferredVendorList;
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
        /// <param name="content">BOM 料項 DM（決策日誌寫入 PendingDecisionLogs）</param>
        /// <returns>客戶承認料清單；null 表示不過濾</returns>
        private async Task<List<string>?> ResolveCustomerApprovedPartsAsync(
            string itemNo,
            string? customerCode,
            BomFileContentDM content)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
            {
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationVariantFilter, "無客戶代碼，不套用客戶承認料過濾");
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
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationVariantFilter, $"客戶代碼 {customerCode} 查無 Variant 設定，不套用過濾");
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
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationVariantFilter,
                    $"套用客戶承認料過濾，客戶代碼：{customerCode}，Variant：{string.Join(", ", variantCodes)}，承認料清單（{approvedParts.Count} 筆）：{string.Join(", ", approvedParts)}");
                return approvedParts;
            }
            else
            {
                content.AddDecisionLog((int)BomFileDecisionLogStageEnum.InternalQuotation, (int)BomFileDecisionLogStepEnum.InternalQuotationVariantFilter,
                    $"客戶代碼 {customerCode} 解析到 Variant 但承認料清單為空，不套用過濾");
                return null;
            }
        }
    }

    /// <summary>
    /// 查料
    /// </summary>
    public partial class QuotationHandler
    {
        /// <summary>
        /// 取得需比對廠牌的料品類別集合（來自系統設定 BrandComparisonCategoryList，Lazy Loading）
        /// </summary>
        /// <returns>需比對廠牌的料品類別代碼集合</returns>
        public HashSet<string> GetBrandComparisonCategorySet()
        {
            if (BrandComparisonCategorySet != null)
                return BrandComparisonCategorySet;

            TBSysSettingBL blTBSysSetting = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
            SearchVO searchVO = new();
            BrandComparisonCategorySet = blTBSysSetting
                .GetListByType(searchVO, ParameterTypeEnum.BrandComparisonCategoryList.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => x.Value!)
                .ToHashSet();
            return BrandComparisonCategorySet;
        }

        /// <summary>
        /// 查料：以三段式（MPN → ComponentPart → Description）決定內部採購型號，
        /// 結果填入 content.No / content.IsRecommendedNo；
        /// 決策歷程累積至 content.PendingDecisionLogs（由 WriteDecisionLog 控制是否記錄）
        /// </summary>
        /// <param name="content">BOM 料項 DM</param>
        /// <param name="brandComparisonCategorySet">需比對廠牌之料品類別集合</param>
        public async Task RunPartSearchAsync(BomFileContentDM content, HashSet<string> brandComparisonCategorySet)
        {
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();
            SanderModuleItemBL blSanderModuleItem = BLFactory.GetInstanceBackGround<SanderModuleItemBL>();

            content.MatchCategory = null;
            content.MatchField = null;

            // 正規化輸入欄位
            string manufacturer = SandermoduleItemNormalizer.Normalize(content.Manufacturer ?? string.Empty);
            string mpn = SandermoduleItemNormalizer.Normalize(content.ManufacturerPartNumber ?? string.Empty);
            string description = SandermoduleItemNormalizer.Normalize(content.Description ?? string.Empty);
            string componentPart = SandermoduleItemNormalizer.Normalize(content.ComponentPart ?? string.Empty);

            // Step1-PhaseA：以 MPN 查詢（此階段不比對廠牌）
            string step1Info = $"查詢值：{mpn}\n";
            if (!string.IsNullOrWhiteSpace(mpn))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(mpn, null);

                if (dmListMatch.Count > 0)
                {
                    dmListMatch = await _extractKeywordHandler.MatchKeyword(mpn, dmListMatch);
                    dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                    // 消歧義：LongDesc 欄位命中優先於 LongDesc2；再依 No 升冪排序
                    if (dmListMatch.Any(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)))
                        dmListMatch = dmListMatch.Where(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)).ToList();
                    dmListMatch = dmListMatch.OrderBy(x => x.No, StringComparer.Ordinal).ToList();

                    step1Info += $"MPN 命中 {dmListMatch.Count} 筆（此階段不比對廠牌），候選清單：{string.Join(", ", dmListMatch.Select(x => $"{x.No}({x.Keyword})"))}\n";

                    // Step1-PhaseB：依 MPN 命中結果決定料號；僅 DIODE 類別才查 LongDesc 是否含符合廠牌字眼
                    string? step1FallbackNo = null;
                    string step1FallbackResult = string.Empty;

                    foreach (TBSanderModuleItemKeywordDM dm in dmListMatch)
                    {
                        if (string.IsNullOrEmpty(dm.No))
                            continue;

                        SanderModuleItemDM? itemDM = blSanderModuleItem.GetOneInfoByNo(dm.No);
                        if (!RequiresDiodeBrandComparison(itemDM, brandComparisonCategorySet))
                        {
                            step1Info += $"[{dm.No}] 非 DIODE 類別，MPN 命中即完全命中，命中欄位：{dm.ColumnName}\n";
                            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep1, step1Info + "查料結果：完全命中（MPN）");
                            content.No = dm.No;
                            content.IsRecommendedNo = false;
                            content.MatchCategory = (int)MatchCategoryEnum.Hit;
                            content.MatchField = "MPN";
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(manufacturer))
                        {
                            step1Info += $"[{dm.No}] DIODE 類別，BOM 廠牌欄空白，列為建議候選\n";
                            if (step1FallbackNo == null)
                            {
                                step1FallbackNo = dm.No;
                                step1FallbackResult = "查料結果：建議料號（MPN 命中，DIODE 類別，廠牌欄空白）";
                            }
                            continue;
                        }

                        bool brandMatched = await HasMatchingManufacturerInLongDescAsync(
                            manufacturer, dm.No, blTBSanderModuleItemKeyword);
                        if (brandMatched)
                        {
                            step1Info += $"[{dm.No}] DIODE 類別，LongDesc 含符合廠牌字眼，完全命中\n";
                            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep1, step1Info + "查料結果：完全命中（MPN + DIODE 廠牌一致）");
                            content.No = dm.No;
                            content.IsRecommendedNo = false;
                            content.MatchCategory = (int)MatchCategoryEnum.Hit;
                            content.MatchField = "MPN";
                            return;
                        }

                        step1Info += $"[{dm.No}] DIODE 類別，LongDesc 未找到符合廠牌字眼，列為建議候選\n";
                        if (step1FallbackNo == null)
                        {
                            step1FallbackNo = dm.No;
                            step1FallbackResult = "查料結果：建議料號（MPN 命中，DIODE 類別，廠牌不符）";
                        }
                    }

                    if (step1FallbackNo != null)
                    {
                        step1Info += $"DIODE 候選皆未完全命中，建議料號：{step1FallbackNo}\n";
                        content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep1, step1Info + step1FallbackResult);
                        content.No = step1FallbackNo;
                        content.IsRecommendedNo = true;
                        content.MatchCategory = (int)MatchCategoryEnum.Recommended;
                        content.MatchField = "MPN";
                        return;
                    }

                    step1Info += "未找到\n";
                }
                else
                {
                    step1Info += "未找到\n";
                }
            }
            else
            {
                step1Info += "MPN 欄位為空白，無法以 MPN 查詢\n";
            }

            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep1, step1Info);

            // Step2：以客戶料號（Component Part）查詢 keyword，失敗時 fallback 主檔字面比對
            string step2Info = $"查詢值：{componentPart}\n";
            if (!string.IsNullOrWhiteSpace(componentPart))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(componentPart, null);

                if (dmListMatch.Count > 0)
                {
                    dmListMatch = await _extractKeywordHandler.MatchKeyword(componentPart, dmListMatch);
                    dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                    // 消歧義：LongDesc 優先，再依 No 升冪排序
                    if (dmListMatch.Any(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)))
                        dmListMatch = dmListMatch.Where(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)).ToList();
                    dmListMatch = dmListMatch.OrderBy(x => x.No, StringComparer.Ordinal).ToList();

                    step2Info += $"keyword 命中 {dmListMatch.Count} 筆，候選清單：{string.Join(", ", dmListMatch.Select(x => x.No))}\n";

                    foreach (TBSanderModuleItemKeywordDM dm in dmListMatch)
                    {
                        if (string.IsNullOrEmpty(dm.No))
                            continue;

                        step2Info += $"[{dm.No}] 完全命中，命中欄位：{dm.ColumnName}\n";
                        content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep2, step2Info + "查料結果：完全命中（Component Part）");
                        content.No = dm.No;
                        content.IsRecommendedNo = false;
                        content.MatchCategory = (int)MatchCategoryEnum.Hit;
                        content.MatchField = "Component Part";
                        return;
                    }

                    step2Info += "keyword 比對未找到\n";
                }
                else
                {
                    step2Info += "keyword 候選 0 筆\n";
                }

                List<SanderModuleItemPartMatchDTO> specFieldMatches = blSanderModuleItem.GetListMatchPartNumberInSpecFields(componentPart);
                if (specFieldMatches.Count > 0)
                {
                    SanderModuleItemPartMatchDTO bestMatch = specFieldMatches[0];
                    step2Info += $"主檔字面命中 {specFieldMatches.Count} 筆，候選清單：{string.Join(", ", specFieldMatches.Select(x => x.No))}\n";
                    step2Info += $"[{bestMatch.No}] 完全命中，命中欄位：{bestMatch.MatchedField}（主檔原文）\n";
                    content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep2, step2Info + "查料結果：完全命中（Component Part，主檔原文）");
                    content.No = bestMatch.No;
                    content.IsRecommendedNo = false;
                    content.MatchCategory = (int)MatchCategoryEnum.Hit;
                    content.MatchField = "Component Part";
                    return;
                }

                step2Info += "主檔字面比對未找到\n";
            }
            else
            {
                step2Info += "Component Part 欄位為空白，無法以 Component Part 查詢\n";
            }

            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep2, step2Info);

            // Step3：以零件規格（Description）查詢
            string step3Info = $"查詢值：{description}\n";
            if (!string.IsNullOrWhiteSpace(description))
            {
                string standardized = await _extractKeywordHandler.StandardizeSearchKeyword(description);
                step3Info += $"AI 標準化後查詢值：{standardized}\n";
                if (string.IsNullOrWhiteSpace(standardized))
                    standardized = description;

                TBSanderModuleItemKeywordDM dmForVector = new();
                dmForVector.Keyword = standardized;
                dmForVector.ColumnName = nameof(SanderModuleItemDM.Description);
                await _manualBatchEmbedding.FillEmbed(new List<TBSanderModuleItemKeywordDM> { dmForVector });

                if (dmForVector.KeywordEmbedding != null)
                {
                    string vectorString = $"[{string.Join(",", dmForVector.KeywordEmbedding.ToArray())}]";
                    List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchDescription(vectorString);

                    if (dmListMatch?.Count > 0)
                    {
                        TBSanderModuleItemKeywordDM dmBest = dmListMatch.First();
                        step3Info += $"以 Description 查詢有 {dmListMatch.Count} 筆結果，建議料號：{dmBest.No}，候選清單：{string.Join(", ", dmListMatch.Select(x => x.No))}\n";
                        content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep3, step3Info + "查料結果：建議料號（Description）");
                        content.No = dmBest.No;
                        content.IsRecommendedNo = true;
                        content.MatchCategory = (int)MatchCategoryEnum.Recommended;
                        content.MatchField = "Description";
                        return;
                    }
                    else
                    {
                        step3Info += "未找到\n";
                    }
                }
                else
                {
                    step3Info += "Description 轉向量失敗，無法以 Description 查詢\n";
                }
            }
            else
            {
                step3Info += "Description 欄位為空白，無法以 Description 查詢\n";
            }

            content.MatchCategory = (int)MatchCategoryEnum.Miss;
            content.MatchField = null;
            content.AddDecisionLog((int)BomFileDecisionLogStageEnum.PartSearch, (int)BomFileDecisionLogStepEnum.PartSearchStep3, step3Info + "查料結果：查無料號");
        }

        /// <summary>
        /// 是否為需比對廠牌的 DIODE 類別（依系統設定 BrandComparisonCategoryList，目前為 DIODE）
        /// </summary>
        private static bool RequiresDiodeBrandComparison(SanderModuleItemDM? item, HashSet<string> brandComparisonCategorySet)
        {
            return item?.ItemCategoryCode != null
                && brandComparisonCategorySet.Contains(item.ItemCategoryCode);
        }

        /// <summary>
        /// 於指定料號的 LongDesc / LongDesc2 關鍵字中，查找是否有與 BOM 廠牌語意相同的字眼
        /// </summary>
        private async Task<bool> HasMatchingManufacturerInLongDescAsync(
            string manufacturer,
            string itemNo,
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword)
        {
            List<TBSanderModuleItemKeywordDM> dmListMatchMfr = blTBSanderModuleItemKeyword.GetListMatchLongDesc(manufacturer, itemNo);
            if (dmListMatchMfr.Count == 0)
                return false;

            dmListMatchMfr = await _extractKeywordHandler.MatchKeyword(manufacturer, dmListMatchMfr);
            return dmListMatchMfr.Any(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1);
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
        /// 需求數量（允許小數；呼叫端對 0&lt;qty&lt;1 應先進成 1）
        /// </summary>
        public decimal? Qty { get; set; }
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

        /// <summary>
        /// 供應商代碼（BuyFromVendorNo）
        /// </summary>
        public string? SupplierCode { get; set; }

        /// <summary>
        /// 採購型號 Description_2
        /// </summary>
        public string? Description2 { get; set; }
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

        /// <summary>
        /// 庫存量
        /// </summary>
        public int? Stock { get; set; }

        /// <summary>
        /// 是否為優先供應商報價
        /// </summary>
        public bool IsPreferred { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
