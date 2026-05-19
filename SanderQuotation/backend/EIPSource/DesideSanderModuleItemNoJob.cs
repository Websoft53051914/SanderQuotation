using backend.AI;
using backend.Common;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using Const;
using static Const.Enums;

namespace backend.EIPSource
{
    /// <summary>
    /// 決定採購型號排程工作
    /// </summary>
    public partial class DesideSanderModuleItemNoJob
    {
        /// <summary>
        /// Log 中的 Controller 名稱，方便識別是哪個工作產生的 Log
        /// </summary>
        private const string LogControllerName = nameof(DesideSanderModuleItemNoJob);

        private readonly ExtractKeywordHandler _extractKeywordHandler;
        private readonly ManualBatchEmbedding _manualBatchEmbedding;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="extractKeywordHandler">AI 關鍵字比對處理器</param>
        /// <param name="manualBatchEmbedding">向量化處理器</param>
        public DesideSanderModuleItemNoJob(ExtractKeywordHandler extractKeywordHandler, ManualBatchEmbedding manualBatchEmbedding)
        {
            _extractKeywordHandler = extractKeywordHandler;
            _manualBatchEmbedding = manualBatchEmbedding;
        }

        /// <summary>
        /// 執行查料工作
        /// 取 ProcessStatus = Transferred(2) 且為 BOM 檔案規則的上傳檔案，對每筆 BomFileContent 執行查料，
        /// 結果寫入 TBBomFileQuotation，全部處理完畢後更新 ProcessStatus = PendingPartSearch(3)
        /// </summary>
        /// <param name="logDM">排程執行紀錄，供呼叫端彙總結果；傳入 null 時略過紀錄更新</param>
        public async Task ExecuteAsync(EsScheduleCycleLogDetailDM logDM = null)
        {
            try
            {
                TableExcelBL blTableExcel = BLFactory.GetInstanceBackGround<TableExcelBL>();
                EsFileTransferUploadBL blEsFileTransferUpload = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();

                // 取得需比對廠牌之料品類別清單（此次執行共用，避免每筆重複查詢）
                TBSysSettingBL blTBSysSetting = BLFactory.GetInstanceBackGround<TBSysSettingBL>();
                string brandComparisonType = ((int)ParameterTypeEnum.BrandComparisonCategoryList).ToString();
                SearchVO brandComparisonSearchVO = new();
                HashSet<string> brandComparisonCategorySet = blTBSysSetting
                    .GetListByType(brandComparisonSearchVO, brandComparisonType)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => x.Value)
                    .ToHashSet();

                // 取得 ProcessStatus = PendingPartSearch 的上傳檔案
                SearchVO uploadSearchVO = new();
                uploadSearchVO.ProcessStatusEq = (int)EsFileTransferUploadProcessStatusEnum.PendingPartSearch;

                List<EsFileTransferUploadDM> uploads = blEsFileTransferUpload.GetListEnabled(uploadSearchVO)
                   .ToList();

                foreach (EsFileTransferUploadDM upload in uploads)
                {
                    await ProcessUploadAsync(upload, brandComparisonCategorySet, logDM);
                }

                if (logDM != null)
                    logDM.JobStatus = logDM.ErrorCount == 0 ? "Success" : logDM.DataCount > 0 ? "PartialFail" : "Failed";
            }
            catch (Exception ex)
            {
                Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
                if (logDM != null)
                {
                    logDM.JobStatus = "Failed";
                    logDM.ErrorMessage = ex.Message;
                }
            }
        }

        /// <summary>
        /// 處理單一上傳檔案的查料作業
        /// </summary>
        /// <param name="upload">上傳檔案 DM</param>
        /// <param name="brandComparisonCategorySet">需比對廠牌之料品類別集合（此次執行共用）</param>
        /// <param name="logDM">排程執行紀錄；傳入 null 時略過紀錄更新</param>
        private async Task ProcessUploadAsync(EsFileTransferUploadDM upload, HashSet<string> brandComparisonCategorySet, EsScheduleCycleLogDetailDM logDM = null)
        {
            try
            {
                BomFileContentBL blBomFileContent = BLFactory.GetInstanceBackGround<BomFileContentBL>();
                HandleQuotationBL blHandleQuotation = BLFactory.GetInstanceBackGround<HandleQuotationBL>();
                TBBomFileDecisionLogBL blTBBomFileDecisionLog = BLFactory.GetInstanceBackGround<TBBomFileDecisionLogBL>();

                List<BomFileContentDM> contents = blBomFileContent.GetListByUploadId(upload.UploadId);
                List<(Guid BomFileContentId, string? No, bool IsRecommendedNo)> results = new();

                foreach (BomFileContentDM content in contents)
                {
                    try
                    {
                        // 執行查料前先移除該料項的舊決策歷程
                        SearchVO deleteLogSearchVO = new();
                        deleteLogSearchVO.BomFileContentIdEq = content.Id;
                        blTBBomFileDecisionLog.DeleteByFilter(deleteLogSearchVO);

                        (string? foundNo, bool isRecommendedNo, string remark) = await GetSandermoduleItemNoAsync(content, brandComparisonCategorySet);
                        results.Add((content.Id, foundNo, isRecommendedNo));
                        if (logDM != null)
                            logDM.DataCount++;
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem($"[查料錯誤] UploadId={upload.UploadId} ContentId={content.Id}\n{ex}", ControllerName: LogControllerName);
                        if (logDM != null)
                            logDM.ErrorCount++;
                    }
                }

                // 全部 BomFileContent 處理完畢，批次儲存查料結果並更新狀態為 PartSearchDone（同一 transaction）
                blHandleQuotation.DoSavePartSearchResult(upload.Id, results);
            }
            catch (Exception ex)
            {
                Method.LogSystem($"[查料流程錯誤] UploadId={upload.UploadId}\n{ex}", ControllerName: LogControllerName);
                if (logDM != null)
                    logDM.ErrorCount++;
            }
        }
    }

    public partial class DesideSanderModuleItemNoJob
    {
        /// <summary>
        /// 取得內部採購型號（三段式：MPN → ComponentPart → Description）
        /// </summary>
        /// <param name="content">BOM 料項</param>
        /// <param name="brandComparisonCategorySet">需比對廠牌之料品類別集合</param>
        /// <returns>(找到的採購型號, 是否為建議料號, 查料過程備註)</returns>
        private async Task<(string? foundNo, bool isRecommended, string remark)> GetSandermoduleItemNoAsync(BomFileContentDM content, HashSet<string> brandComparisonCategorySet)
        {
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();
            SanderModuleItemBL blSanderModuleItem = BLFactory.GetInstanceBackGround<SanderModuleItemBL>();
            TBBomFileDecisionLogBL blTBBomFileDecisionLog = BLFactory.GetInstanceBackGround<TBBomFileDecisionLogBL>();

            // 正規化輸入欄位
            string manufacturer = SandermoduleItemNormalizer.Normalize(content.Manufacturer ?? string.Empty);
            string mpn = SandermoduleItemNormalizer.Normalize(content.ManufacturerPartNumber ?? string.Empty);
            string description = SandermoduleItemNormalizer.Normalize(content.Description ?? string.Empty);
            string componentPart = SandermoduleItemNormalizer.Normalize(content.ComponentPart ?? string.Empty);

            string remark = string.Empty;

            // Step1：以 Manufacturer Part Number（MPN）查詢
            remark += $"查詢值：{mpn}\n";
            if (!string.IsNullOrWhiteSpace(mpn))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(mpn, null);
                remark += $"以 MPN 查詢有 {dmListMatch.Count} 筆結果\n";

                if (dmListMatch.Count > 0)
                {
                    dmListMatch = await _extractKeywordHandler.MatchKeyword(mpn, dmListMatch);
                    dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                    // 消歧義（§4.2 Step1 補充）：
                    // 第一層：LongDesc 欄位命中優先於 LongDesc2
                    if (dmListMatch.Any(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)))
                        dmListMatch = dmListMatch.Where(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)).ToList();
                    // 第二層：依 No 字串升冪排序（locale-insensitive）
                    dmListMatch = dmListMatch.OrderBy(x => x.No, StringComparer.Ordinal).ToList();

                    remark += $"命中 {dmListMatch.Count} 筆，候選清單：{string.Join(", ", dmListMatch.Select(x => x.No))}\n";

                    string? step1FallbackNo = null;
                    string step1FallbackResult = string.Empty;

                    foreach (TBSanderModuleItemKeywordDM dm in dmListMatch)
                    {
                        if (string.IsNullOrEmpty(dm.No))
                            continue;

                        SanderModuleItemDM? itemDM = blSanderModuleItem.GetOneInfoByNo(dm.No);
                        bool needsBrandComparison =
                            itemDM?.ItemCategoryCode != null &&
                            brandComparisonCategorySet.Contains(itemDM.ItemCategoryCode);

                        if (!needsBrandComparison)
                        {
                            remark += $"[{dm.No}] 非需比對廠牌類別，完全命中，命中欄位：{dm.ColumnName}\n";
                            string step1FullHitRemark = remark + "查料結果：完全命中（MPN，非需比對廠牌類別）";
                            InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep1, step1FullHitRemark);
                            return (dm.No, false, step1FullHitRemark);
                        }
                        else if (string.IsNullOrWhiteSpace(manufacturer))
                        {
                            remark += $"[{dm.No}] 需比對廠牌類別，廠牌欄空白，列為建議候選\n";
                            if (step1FallbackNo == null)
                            {
                                step1FallbackNo = dm.No;
                                step1FallbackResult = "查料結果：建議料號（MPN，需比對廠牌類別，廠牌欄空白）";
                            }
                        }
                        else
                        {
                            List<TBSanderModuleItemKeywordDM> dmListMatchMfr = blTBSanderModuleItemKeyword.GetListMatchLongDesc(manufacturer, dm.No);
                            dmListMatchMfr = await _extractKeywordHandler.MatchKeyword(manufacturer, dmListMatchMfr);
                            dmListMatchMfr = dmListMatchMfr.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                            if (dmListMatchMfr.Count > 0)
                            {
                                remark += $"[{dm.No}] 需比對廠牌類別，廠牌一致，完全命中\n";
                                string step1FullHitMfrRemark = remark + "查料結果：完全命中（MPN，需比對廠牌類別，廠牌一致）";
                                InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep1, step1FullHitMfrRemark);
                                return (dm.No, false, step1FullHitMfrRemark);
                            }
                            else
                            {
                                remark += $"[{dm.No}] 需比對廠牌類別，廠牌不符，列為建議候選\n";
                                if (step1FallbackNo == null)
                                {
                                    step1FallbackNo = dm.No;
                                    step1FallbackResult = "查料結果：建議料號（MPN，需比對廠牌類別，廠牌不符）";
                                }
                            }
                        }
                    }

                    if (step1FallbackNo != null)
                    {
                        remark += $"所有候選皆非完全命中，建議料號：{step1FallbackNo}\n";
                        string step1SuggestRemark = remark + step1FallbackResult;
                        InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep1, step1SuggestRemark);
                        return (step1FallbackNo, true, step1SuggestRemark);
                    }

                    remark += "未找到\n";
                }
                else
                {
                    remark += "未找到\n";
                }
            }
            else
            {
                remark += "MPN 欄位為空白，無法以 MPN 查詢\n";
            }

            InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep1, remark);

            // Step2：以客戶料號（Component Part）查詢
            remark += "Step2：以客戶料號（Component Part）查詢\n";
            remark += $"查詢值：{componentPart}\n";
            if (!string.IsNullOrWhiteSpace(componentPart))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(componentPart, null);
                remark += $"以 Component Part 查詢有 {dmListMatch.Count} 筆結果\n";

                if (dmListMatch.Count > 0)
                {
                    dmListMatch = await _extractKeywordHandler.MatchKeyword(componentPart, dmListMatch);
                    dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                    // 消歧義（同 Step1）：LongDesc 優先，再依 No 升冪排序
                    if (dmListMatch.Any(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)))
                        dmListMatch = dmListMatch.Where(x => x.ColumnName == nameof(SanderModuleItemDM.LongDesc)).ToList();
                    dmListMatch = dmListMatch.OrderBy(x => x.No, StringComparer.Ordinal).ToList();

                    remark += $"命中 {dmListMatch.Count} 筆，候選清單：{string.Join(", ", dmListMatch.Select(x => x.No))}\n";

                    foreach (TBSanderModuleItemKeywordDM dm in dmListMatch)
                    {
                        if (string.IsNullOrEmpty(dm.No))
                            continue;

                        remark += $"[{dm.No}] 完全命中，命中欄位：{dm.ColumnName}\n";
                        string step2Remark = remark + "查料結果：完全命中（Component Part）";
                        InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep2, step2Remark);
                        return (dm.No, false, step2Remark);
                    }

                    remark += "未找到\n";
                }
                else
                {
                    remark += "未找到\n";
                }
            }
            else
            {
                remark += "Component Part 欄位為空白，無法以 Component Part 查詢\n";
            }

            InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep2, remark);

            // Step3：以零件規格（Description）查詢
            remark += $"查詢值：{description}\n";
            if (!string.IsNullOrWhiteSpace(description))
            {
                string standardized = await _extractKeywordHandler.StandardizeSearchKeyword(description);
                remark += $"AI 標準化後查詢值：{standardized}\n";
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
                        remark += $"以 Description 查詢有 {dmListMatch.Count} 筆結果，建議料號：{dmBest.No}，候選清單：{string.Join(", ", dmListMatch.Select(x => x.No))}\n";
                        string step3Remark = remark + "查料結果：建議料號（Description）";
                        InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep3, step3Remark);
                        return (dmBest.No, true, step3Remark);
                    }
                    else
                    {
                        remark += "未找到\n";
                    }
                }
                else
                {
                    remark += "Description 轉向量失敗，無法以 Description 查詢\n";
                }
            }
            else
            {
                remark += "Description 欄位為空白，無法以 Description 查詢\n";
            }

            string notFoundRemark = remark + "查料結果：查無料號";
            InsertDecisionLog(blTBBomFileDecisionLog, content.Id, BomFileDecisionLogStageEnum.PartSearch, BomFileDecisionLogStepEnum.PartSearchStep3, notFoundRemark);
            return (null, false, notFoundRemark);
        }

        /// <summary>
        /// 寫入一筆查料決策歷程紀錄至資料庫
        /// </summary>
        /// <param name="blTBBomFileDecisionLog">決策歷程 BL 實例</param>
        /// <param name="bomFileContentId">BOM 料項 Id</param>
        /// <param name="stage">決策階段</param>
        /// <param name="step">決策步驟</param>
        /// <param name="message">決策訊息</param>
        private void InsertDecisionLog(
            TBBomFileDecisionLogBL blTBBomFileDecisionLog,
            Guid bomFileContentId,
            BomFileDecisionLogStageEnum stage,
            BomFileDecisionLogStepEnum step,
            string message)
        {
            TBBomFileDecisionLogDM logDM = new();
            logDM.BomFileContentId = bomFileContentId;
            logDM.Stage = (int)stage;
            logDM.Step = (int)step;
            logDM.Message = message;
            blTBBomFileDecisionLog.DoInsert(logDM);
        }
    }
}

