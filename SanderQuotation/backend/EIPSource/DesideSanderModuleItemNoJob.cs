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
        public async Task ExecuteAsync()
        {
            try
            {
                TableExcelBL blTableExcel = BLFactory.GetInstanceBackGround<TableExcelBL>();
                EsFileTransferUploadBL blEsFileTransferUpload = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();

                // 取得 ProcessStatus = PendingPartSearch 的上傳檔案
                SearchVO uploadSearchVO = new();
                uploadSearchVO.ProcessStatusEq = (int)EsFileTransferUploadProcessStatusEnum.PendingPartSearch;

                List<EsFileTransferUploadDM> uploads = blEsFileTransferUpload.GetListEnabled(uploadSearchVO)
                   .ToList();

                foreach (EsFileTransferUploadDM upload in uploads)
                {
                    await ProcessUploadAsync(upload);
                }
            }
            catch (Exception ex)
            {
                Method.LogSystem(ex.ToString(), ControllerName: LogControllerName);
            }
        }

        /// <summary>
        /// 處理單一上傳檔案的查料作業
        /// </summary>
        /// <param name="upload">上傳檔案 DM</param>
        private async Task ProcessUploadAsync(EsFileTransferUploadDM upload)
        {
            try
            {
                BomFileContentBL blBomFileContent = BLFactory.GetInstanceBackGround<BomFileContentBL>();
                HandleQuotationBL blHandleQuotation = BLFactory.GetInstanceBackGround<HandleQuotationBL>();

                List<BomFileContentDM> contents = blBomFileContent.GetListByUploadId(upload.UploadId);
                List<(Guid BomFileContentId, string? No)> results = new();

                foreach (BomFileContentDM content in contents)
                {
                    try
                    {
                        (string? foundNo, string remark) = await GetSandermoduleItemNoAsync(content);
                        Method.LogSystem($"[查料] UploadId={upload.UploadId} ContentId={content.Id} No={foundNo}\n{remark}", ControllerName: LogControllerName);
                        results.Add((content.Id, foundNo));
                    }
                    catch (Exception ex)
                    {
                        Method.LogSystem($"[查料錯誤] UploadId={upload.UploadId} ContentId={content.Id}\n{ex}", ControllerName: LogControllerName);
                    }
                }

                // 全部 BomFileContent 處理完畢，批次儲存查料結果並更新狀態為 PartSearchDone（同一 transaction）
                blHandleQuotation.DoSavePartSearchResult(upload.Id, results);
            }
            catch (Exception ex)
            {
                Method.LogSystem($"[查料流程錯誤] UploadId={upload.UploadId}\n{ex}", ControllerName: LogControllerName);
            }
        }
    }

    public partial class DesideSanderModuleItemNoJob
    {
        /// <summary>
        /// 取得內部採購型號（三段式：MPN → ComponentPart → Description）
        /// </summary>
        /// <param name="content">BOM 料項</param>
        /// <returns>(找到的採購型號, 查料過程備註)</returns>
        private async Task<(string? foundNo, string remark)> GetSandermoduleItemNoAsync(BomFileContentDM content)
        {
            TBSanderModuleItemKeywordBL blTBSanderModuleItemKeyword = BLFactory.GetInstanceBackGround<TBSanderModuleItemKeywordBL>();

            // 正規化輸入欄位
            string manufacturer = SandermoduleItemNormalizer.Normalize(content.Manufacturer ?? string.Empty);
            string mpn = SandermoduleItemNormalizer.Normalize(content.ManufacturerPartNumber ?? string.Empty);
            string description = SandermoduleItemNormalizer.Normalize(content.Description ?? string.Empty);
            string componentPart = SandermoduleItemNormalizer.Normalize(content.ComponentPart ?? string.Empty);

            string remark = string.Empty;

            // Step1：以 Manufacturer Part Number（MPN）查詢
            remark += "Step1：以 Manufacturer Part Number（MPN）查詢\n";
            remark += $"查詢值：{mpn}\n";
            if (!string.IsNullOrWhiteSpace(mpn))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(mpn, null);
                remark += $"以 MPN 查詢有 {dmListMatch.Count} 筆結果\n";

                if (dmListMatch.Count > 0)
                {
                    dmListMatch = await _extractKeywordHandler.MatchKeyword(mpn, dmListMatch);
                    dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                    string? fallbackDiodeNo = null;
                    foreach (TBSanderModuleItemKeywordDM dm in dmListMatch)
                    {
                        if (string.IsNullOrEmpty(dm.No))
                            continue;

                        if (!dm.No.StartsWith("34-"))
                        {
                            remark += $"非二極體類，完全命中，料號：{dm.No}\n";
                            return (dm.No, remark + "查料結果：完全命中（MPN，非二極體類）");
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(manufacturer))
                            {
                                remark += $"二極體類，廠牌欄空白，建議料號：{dm.No}\n";
                                return (dm.No, remark + "查料結果：建議料號（MPN，二極體類，廠牌欄空白）");
                            }
                            else
                            {
                                List<TBSanderModuleItemKeywordDM> dmListMatchMfr = blTBSanderModuleItemKeyword.GetListMatchLongDesc(manufacturer, dm.No);
                                dmListMatchMfr = await _extractKeywordHandler.MatchKeyword(manufacturer, dmListMatchMfr);
                                dmListMatchMfr = dmListMatchMfr.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                                if (dmListMatchMfr.Count > 0)
                                {
                                    remark += $"二極體類，廠牌一致，完全命中，料號：{dm.No}\n";
                                    return (dm.No, remark + "查料結果：完全命中（MPN，二極體類，廠牌一致）");
                                }
                                else
                                {
                                    remark += $"二極體類，廠牌不符，記錄備用料號：{dm.No}\n";
                                    fallbackDiodeNo ??= dm.No;
                                }
                            }
                        }
                    }

                    if (fallbackDiodeNo != null)
                    {
                        remark += $"二極體類，所有結果廠牌不符，建議料號：{fallbackDiodeNo}\n";
                        return (fallbackDiodeNo, remark + "查料結果：建議料號（MPN，二極體類，廠牌不符）");
                    }
                }
                else
                {
                    remark += "以 MPN 查詢無結果\n";
                }
            }
            else
            {
                remark += "MPN 欄位為空白，無法以 MPN 查詢\n";
            }

            // Step2：以客戶料號（Component Part）查詢
            remark += "Step2：以客戶料號（Component Part）查詢\n";
            remark += $"查詢值：{componentPart}\n";
            if (!string.IsNullOrWhiteSpace(componentPart))
            {
                List<TBSanderModuleItemKeywordDM> dmListMatch = blTBSanderModuleItemKeyword.GetListMatchLongDesc(componentPart, null);
                remark += $"以 Component Part 查詢有 {dmListMatch.Count} 筆結果\n";
                dmListMatch = await _extractKeywordHandler.MatchKeyword(componentPart, dmListMatch);
                dmListMatch = dmListMatch.Where(x => x.SimilarityScore.HasValue && x.SimilarityScore == 1).ToList();

                TBSanderModuleItemKeywordDM? dmMatch = dmListMatch.FirstOrDefault();
                if (dmMatch != null)
                {
                    remark += $"完全命中，料號：{dmMatch.No}\n";
                    return (dmMatch.No, remark + "查料結果：完全命中（Component Part）");
                }
                else
                {
                    remark += "以 Component Part 查詢無結果\n";
                }
            }
            else
            {
                remark += "Component Part 欄位為空白，無法以 Component Part 查詢\n";
            }

            // Step3：以零件規格（Description）查詢
            remark += "Step3：以零件規格（Description）查詢\n";
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
                        remark += $"以 Description 查詢有 {dmListMatch.Count} 筆結果，建議料號：{dmBest.No}\n";
                        return (dmBest.No, remark + "查料結果：建議料號（Description）");
                    }
                    else
                    {
                        remark += "以 Description 查詢無結果\n";
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

            return (null, remark + "查料結果：查無料號");
        }
    }
}

