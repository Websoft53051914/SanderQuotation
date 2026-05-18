using backend.EIPSource;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Utility;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Const.Enums;

namespace backend.Common
{

    public partial class TransferJob
    {
        private IConfiguration _config;

        private SynoNasHelper _nasHelper;

        private EsFileTransferMappingDM _mapping;
        private readonly Dictionary<string, ESDbTransferDM> _dbConfigCache = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TransferJob(IConfiguration config
            , IServiceScopeFactory scopeFactory
            , SynoNasHelper nasHelper
            , IWebHostEnvironment webHostEnvironment)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            _nasHelper = nasHelper;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task ExecuteTask(string scheduleCycleCode, string TriggerType)
        {
            EsScheduleCycleLogDM log = new();
            try
            {
                EsScheduleCycleBL bl = BLFactory.GetInstanceBackGround<EsScheduleCycleBL>();
                var data = bl.GetByCode(scheduleCycleCode);
                if (data == null || data.Status != StatusEnum.Enabled.ToValueString() || (data.DBTransferSettings.Count == 0 && data.FileTransferSettings.Count == 0 && data.DbCsvTransferSettings.Count == 0 && data.OtherTransferSettings.Count == 0))
                {
                    // log not found
                    return;
                }

                DateTime st = DateTime.Now;

                log.RunAt = st;

                var dbTasks = data.DBTransferSettings.Select(setting => DBTransfer(setting));
                var fileTasks = data.FileTransferSettings.Select(setting => FileTransfer(setting));
                var otherTasks = data.OtherTransferSettings.Select(setting => OtherTransfer(setting));
                //var dbCsvTasks = data.DbCsvTransferSettings.Select(setting => DBToCSVTransfer(setting));

                var dbResults = await Task.WhenAll(dbTasks);
                var fileResults = await Task.WhenAll(fileTasks);
                var otherResults = await Task.WhenAll(otherTasks);
                //var dbCsvResults = await Task.WhenAll(dbCsvTasks);

                log.ScheduleCycleCode = scheduleCycleCode;
                log.DurationMs = (int)(DateTime.Now - st).TotalMilliseconds;
                log.Details = dbResults.Concat(fileResults).Concat(otherResults).ToList();
                log.TriggerType = TriggerType;
                EsScheduleCycleLogBL esScheduleCycleLogBL = BLFactory.GetInstanceBackGround<EsScheduleCycleLogBL>();
                esScheduleCycleLogBL.InserLog(log);

            }
            catch (Exception ex)
            {
                // TODO: log ex
                throw;
            }
            finally
            {
                await DoCompleted();
            }
        }

        private Task DoCompleted()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// DB轉入
        /// </summary>
        /// <param name="transferCode"></param>
        /// <returns></returns>
        public async Task<EsScheduleCycleLogDetailDM> DBTransfer(string transferCode)
        {
            DateTime st = DateTime.Now;

            EsScheduleCycleLogDetailDM logDM = new();
            logDM.DBTransferCode = transferCode;
            logDM.DataCount = 0;
            logDM.RunAt = st;

            try
            {
                ESDbTransferMappingBL eSDbTransferMappingBL = BLFactory.GetInstanceBackGround<ESDbTransferMappingBL>();
                var dm = eSDbTransferMappingBL.GetByCode(transferCode);

                var secretKey = _config["SecretKey"];
                var secretIV = _config["SecretIV"];

                var exeLogDM = eSDbTransferMappingBL.ExecuteTransfer(dm.Id, secretKey, secretIV);

                // ExecuteTransfer 內部已設定 JobStatus=Failed 代表基礎設施層級錯誤
                if (exeLogDM.JobStatus == "Failed")
                {
                    logDM.JobStatus = "Failed";
                    logDM.ErrorMessage = exeLogDM.ErrorMessage;
                }
                else
                {
                    logDM.ErrorLogs = exeLogDM.ErrorLogs;
                    logDM.DataCount = exeLogDM.DataCount;
                    logDM.ErrorCount = exeLogDM.ErrorCount;
                    logDM.JobStatus = logDM.ErrorCount > 0 ? "PartialFail" : "Success";
                }
            }
            catch (Exception ex)
            {
                logDM.JobStatus = "Failed";
                logDM.ErrorMessage = ex.Message;
            }
            finally
            {
                logDM.DurationMs = (int)(DateTime.Now - st).TotalMilliseconds;
            }

            return logDM;
        }

        /// <summary>
        /// NAS 檔案轉入：根據轉入代碼取得設定，從 NAS 讀取並轉入。
        /// </summary>
        /// <param name="transferCode">檔案轉入代碼</param>
        /// <returns>執行結果</returns>
        /// <exception cref="InvalidOperationException">找不到轉入設定時擲出</exception>
        public async Task<EsScheduleCycleLogDetailDM> FileTransfer(string transferCode)
        {
            EsScheduleCycleLogDetailDM logDM = new();
            logDM.FileTransferCode = transferCode;
            logDM.DataCount = 0;

            TableExcelBL bl = BLFactory.GetInstanceBackGround<TableExcelBL>();
            _mapping = bl.GetByCode(transferCode)
                ?? throw new InvalidOperationException($"找不到檔案轉入設定：{transferCode}");

            //logDM = await RunNasTransfer();
            logDM = await RunLocalTransfer();

            return logDM;
        }

        /// <summary>
        /// 本地檔案轉入：根據轉入代碼取得設定，從本地上傳目錄讀取並轉入。
        /// </summary>
        /// <param name="transferCode">檔案轉入代碼</param>
        /// <returns>執行結果</returns>
        /// <exception cref="InvalidOperationException">找不到轉入設定時擲出</exception>
        public Task<EsScheduleCycleLogDetailDM> LocalFileTransfer(string transferCode)
        {
            EsScheduleCycleLogDetailDM logDM = new();
            logDM.FileTransferCode = transferCode;
            logDM.DataCount = 0;

            TableExcelBL bl = BLFactory.GetInstanceBackGround<TableExcelBL>();
            _mapping = bl.GetByCode(transferCode)
                ?? throw new InvalidOperationException($"找不到檔案轉入設定：{transferCode}");

            return RunLocalTransfer();
        }
    }

    /// <summary>
    /// Nas檔案轉入資料庫
    /// </summary>
    public partial class TransferJob
    {
        /// <summary>
        /// 從 NAS 讀取符合設定副檔名的檔案，逐一執行轉入，成功後將檔案移至完成資料夾
        /// </summary>
        private async Task<EsScheduleCycleLogDetailDM> RunNasTransfer()
        {
            var logDM = new EsScheduleCycleLogDetailDM
            {
                FileTransferCode = _mapping.TransferMappingCode,
                RunAt = DateTime.Now
            };

            try
            {
                var configError = ValidateNasConfig(out var nasUrl, out var nasAccount, out var nasPassword, out var srcPath, out var finishPath);
                if (configError != null)
                    return FailedResult(logDM, configError);

                var (context, loginError) = await ConnectNasAsync(nasUrl, nasAccount, nasPassword);
                if (loginError != null)
                    return FailedResult(logDM, loginError);

                var (matchedFiles, listError) = await FetchMatchedFilesAsync(context, srcPath);
                if (listError != null)
                    return FailedResult(logDM, listError);

                if (matchedFiles.Count == 0)
                {
                    logDM.JobStatus = "Success";
                    return logDM;
                }

                var successPaths = await ProcessFilesAsync(context, matchedFiles, logDM);

                await MoveSuccessFilesAsync(context, successPaths, finishPath);

                // 7. 刪除完成資料夾中的所有檔案（保留資料夾本身）
                //await ClearNasFinishFolderAsync(context, finishPath);

                logDM.JobStatus = ResolveJobStatus(logDM.DataCount, logDM.ErrorCount);
            }
            catch (Exception ex)
            {
                //LogError(ex);
                logDM.JobStatus = "Failed";
                logDM.ErrorMessage = ex.Message;
            }
            finally
            {
                logDM.DurationMs = (int)(DateTime.Now - logDM.RunAt).TotalMilliseconds;
            }

            return logDM;
        }

        /// <summary>
        /// 驗證 NAS 相關設定是否齊全，同時輸出各設定值。
        /// 回傳 null 表示驗證通過；否則回傳錯誤訊息。
        /// </summary>
        private string ValidateNasConfig(
            out string nasUrl,
            out string nasAccount,
            out string nasPassword,
            out string srcPath,
            out string finishPath)
        {
            nasUrl = _config.GetValue<string>("Nas:Url");
            nasAccount = _config.GetValue<string>("Nas:Account");
            nasPassword = _config.GetValue<string>("Nas:Password");
            srcPath = _mapping.SrcNasFilePath?.TrimEnd('/');
            finishPath = _config.GetValue<string>("Nas:TransferFinishPath");

            if (string.IsNullOrWhiteSpace(nasUrl)) return "appsettings.json 缺少 Nas:Url 設定";
            if (string.IsNullOrWhiteSpace(nasAccount)) return "appsettings.json 缺少 Nas:Account 設定";
            if (string.IsNullOrWhiteSpace(srcPath)) return "EsFileTransferMappingDM.SrcNasFilePath 未設定";
            if (string.IsNullOrWhiteSpace(finishPath)) return "appsettings.json 缺少 Nas:TransferFinishPath 設定";

            return null;
        }

        /// <summary>
        /// 登入 NAS 並建立連線 context。
        /// 回傳 (context, null) 表示成功；(null, errorMessage) 表示失敗。
        /// </summary>
        private async Task<(SynoNasConnectionContext context, string error)> ConnectNasAsync(
            string nasUrl, string nasAccount, string nasPassword)
        {
            var loginResp = await _nasHelper.LoginAsync(nasUrl, nasAccount, nasPassword);
            if (!loginResp.Success)
                return (null, $"NAS 登入失敗：{loginResp.Error}");

            var context = new SynoNasConnectionContext
            {
                Url = nasUrl.TrimEnd('/'),
                Sid = loginResp.Sid,
                Account = nasAccount,
                Password = nasPassword,
            };

            return (context, null);
        }

        /// <summary>
        /// 列出 NAS 來源資料夾中符合副檔名的檔案。
        /// 回傳 (files, null) 表示成功；(null, errorMessage) 表示失敗。
        /// </summary>
        private async Task<(List<FileItem> files, string error)> FetchMatchedFilesAsync(
            SynoNasConnectionContext context, string srcPath)
        {
            var listResp = await _nasHelper.FileStation_List(context, new FileStationListRequest { FolderPath = srcPath });
            if (!listResp.Success)
                return (null, $"列出 NAS 目錄失敗：{listResp.Error}");

            var allowedExt = GetAllowedExtension(_mapping.ExampleFileType);
            var matched = listResp.Data?.files?
                .Where(f => !f.isdir &&
                            string.Equals(Path.GetExtension(f.name), allowedExt, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<FileItem>();

            return (matched, null);
        }

        /// <summary>
        /// 逐一下載 NAS 檔案至本機暫存目錄並執行轉入，彙總結果至 logDM。
        /// 回傳全部轉入成功的 NAS 檔案路徑清單。
        /// </summary>
        private async Task<List<string>> ProcessFilesAsync(
            SynoNasConnectionContext context,
            List<FileItem> files,
            EsScheduleCycleLogDetailDM logDM)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "NasTransfer", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var successPaths = new List<string>();

            try
            {
                foreach (var file in files)
                {
                    var tempFilePath = Path.Combine(tempDir, file.name);
                    try
                    {
                        // 下載 NAS 檔案到暫存目錄
                        using var stream = await _nasHelper.FileStation_Download(context,
                            new FileStationDownloadRequest { path = file.path });
                        await using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await stream.CopyToAsync(fs);
                        }

                        // 執行轉入並彙總結果
                        var transferResult = Execute(tempFilePath);
                        logDM.DataCount += transferResult.DataCount;
                        logDM.ErrorCount += transferResult.ErrorCount;
                        logDM.ErrorLogs.AddRange(transferResult.ErrorLogs);

                        // 僅全成功（ErrorCount == 0）的檔案才移至完成資料夾
                        if (transferResult.ErrorCount == 0)
                            successPaths.Add(file.path);
                    }
                    catch (Exception ex)
                    {
                        //LogError(ex);
                        logDM.ErrorCount++;
                        logDM.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = ex.Message });
                    }
                    finally
                    {
                        try { if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath); } catch { }
                    }
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }

            return successPaths;
        }

        /// <summary>
        /// 將轉入成功的 NAS 檔案逐一移動至完成資料夾。
        /// 移動失敗不影響整體結果。
        /// </summary>
        private async Task MoveSuccessFilesAsync(
            SynoNasConnectionContext context,
            List<string> successPaths,
            string finishPath)
        {
            foreach (var nasFilePath in successPaths)
            {
                try
                {
                    await MoveNasFileAsync(context, nasFilePath, finishPath);
                }
                catch (Exception ex)
                {
                    //LogError(ex);
                    // 移動失敗不影響整體結果，繼續處理其他檔案
                }
            }
        }

        /// <summary>
        /// 將單一 NAS 檔案移動至目標資料夾。
        /// 若目標已存在同名檔案，先刪除再移動；移動後輪詢直到完成（最多等 60 秒）。
        /// </summary>
        private async Task MoveNasFileAsync(
            SynoNasConnectionContext context,
            string nasFilePath,
            string finishPath)
        {
            var fileName = nasFilePath.Contains('/')
                ? nasFilePath[(nasFilePath.LastIndexOf('/') + 1)..]
                : nasFilePath;
            var destFilePath = finishPath.TrimEnd('/') + "/" + fileName;

            // 若目標資料夾已存在同名檔案，先刪除再移動
            var checkResp = await _nasHelper.FileStation_List(context, new FileStationListRequest { FolderPath = finishPath });
            if (checkResp.Success &&
                checkResp.Data?.files?.Any(f => string.Equals(f.name, fileName, StringComparison.OrdinalIgnoreCase)) == true)
            {
                await _nasHelper.FileStation_Delete(context, new FileStationDeleteRequest
                {
                    path = new List<string> { destFilePath },
                    recursive = false
                });
            }

            var startData = await _nasHelper.CopyMove_StartAsync(
                context,
                new List<string> { nasFilePath },
                finishPath,
                removeSrc: true);

            // 輪詢直到完成（最多等 60 秒）
            for (int i = 0; i < 120; i++)
            {
                await Task.Delay(500);
                var statusData = await _nasHelper.CopyMove_StatusAsync(context, startData.taskid);
                if (statusData.finished) break;
            }
        }

        /// <summary>
        /// 依副檔名類型代碼回傳對應的副檔名。
        /// </summary>
        private static string GetAllowedExtension(int fileType) => fileType switch
        {
            1 => ".xlsx",
            2 => ".xls",
            _ => ".csv"
        };

        /// <summary>
        /// 依資料筆數與錯誤筆數決定最終 JobStatus。
        /// </summary>
        private static string ResolveJobStatus(int dataCount, int errorCount) =>
            dataCount == 0 ? "Failed" : errorCount == 0 ? "Success" : "PartialFail";

        /// <summary>
        /// 設定 logDM 為失敗狀態並回傳。
        /// </summary>
        private static EsScheduleCycleLogDetailDM FailedResult(EsScheduleCycleLogDetailDM logDM, string errorMessage)
        {
            logDM.JobStatus = "Failed";
            logDM.ErrorMessage = errorMessage;
            return logDM;
        }

        /// <summary>
        /// 刪除 NAS 完成資料夾中的所有檔案與子資料夾，但保留資料夾本身
        /// </summary>
        private async Task ClearNasFinishFolderAsync(SynoNasConnectionContext context, string finishPath)
        {
            try
            {
                var finishListResp = await _nasHelper.FileStation_List(context, new FileStationListRequest { FolderPath = finishPath });
                if (finishListResp.Success && finishListResp.Data?.files?.Count > 0)
                {
                    var pathsToDelete = finishListResp.Data.files
                        .Select(f => f.path)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();
                    if (pathsToDelete.Count > 0)
                    {
                        await _nasHelper.FileStation_Delete(context, new FileStationDeleteRequest
                        {
                            path = pathsToDelete,
                            recursive = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //LogError(ex);
                // 清除完成資料夾失敗不影響整體結果
            }
        }

        /// <summary>
        /// 執行檔案轉入，回傳執行結果
        /// </summary>
        private EsScheduleCycleLogDetailDM Execute(string filePath = null)
        {
            var result = new EsScheduleCycleLogDetailDM
            {
                FileTransferCode = _mapping.TransferMappingCode,
                RunAt = DateTime.Now
            };

            try
            {
                // 驗證設定
                if (_mapping.Columns == null || _mapping.Columns.Count == 0)
                    return ErrorResult(_mapping.TransferMappingCode, "未設定欄位對應清單");

                // 取得實體檔案路徑
                filePath ??= BuildFilePath();
                if (!System.IO.File.Exists(filePath))
                    return ErrorResult(_mapping.TransferMappingCode, $"NAS 檔案不存在：{filePath}");

                // 依檔案類型分派解析
                switch (_mapping.ExampleFileType)
                {
                    case 1: // xlsx
                    case 2: // xls
                        ProcessExcel(filePath, result);
                        break;
                    case 0: // CSV
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                result = ErrorResult(_mapping.TransferMappingCode, ex.Message);
            }

            return result;
        }

        // ─── 檔案路徑 ────────────────────────────────────────────────

        private string BuildFilePath()
        {
            return @"C:\Users\websoft\Downloads\sysTpcLib_202604201828.xlsx";
        }

        // ─── Excel 解析 ───────────────────────────────────────────────

        private void ProcessExcel(string filePath, EsScheduleCycleLogDetailDM result)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = _mapping.ExampleFileType == 1
                ? (IWorkbook)new XSSFWorkbook(stream)   // xlsx
                : new HSSFWorkbook(stream);              // xls

            // 依 SrcSheetIndex 分組（同一工作表可能對應多個 TargetTable，但 HeaderRowIndex 相同）
            var sheetGroups = _mapping.Columns
                .GroupBy(c => c.SrcSheetIndex)
                .OrderBy(g => g.Key);

            foreach (var sheetGroup in sheetGroups)
            {
                int sheetIndex = sheetGroup.Key;
                ISheet sheet;
                try
                {
                    // R02: SrcSheetIndex 優先，SrcSheetName 備用
                    sheet = (sheetIndex >= 0 && sheetIndex < workbook.NumberOfSheets)
                        ? workbook.GetSheetAt(sheetIndex)
                        : workbook.GetSheet(sheetGroup.First().SrcSheetName);
                }
                catch
                {
                    result.ErrorCount++;
                    result.ErrorLogs.Add(new EsTransferErrorLogDM
                    {
                        Exception = $"工作表索引 {sheetIndex} 不存在"
                    });
                    continue;
                }

                if (sheet == null)
                {
                    result.ErrorCount++;
                    result.ErrorLogs.Add(new EsTransferErrorLogDM
                    {
                        Exception = $"找不到工作表：index={sheetIndex}, name={sheetGroup.First().SrcSheetName}"
                    });
                    continue;
                }

                // R03: HeaderRowIndex 是 1-based
                int headerRowIndex = sheetGroup.First().HeaderRowIndex - 1; // 轉為 0-based
                IRow headerRow = sheet.GetRow(headerRowIndex);
                if (headerRow == null) continue;

                // 建立標題 → 欄索引的對應
                var headerMap = BuildHeaderMap(headerRow);

                // 資料從 HeaderRowIndex + 1 開始（0-based: headerRowIndex + 1）
                int dataStartRow = headerRowIndex + 1;

                // 依 (TargetTableName, DBTransferMappingCode) 再分組，支援一張 Sheet 寫入多資料表
                var tableGroups = sheetGroup
                    .GroupBy(c => (c.TargetTableName, c.DBTransferMappingCode))
                    .ToList();

                // 每個 tableGroup 只開啟一次資料庫連線，供整張 Sheet 所有列共用
                Dictionary<(string, string), (DbConnection conn, IDbDialect dialect)> tableConnections = new();
                try
                {
                    foreach (var tg in tableGroups)
                    {
                        ESDbTransferDM dbConfig = GetOrCacheDbConfig(tg.Key.DBTransferMappingCode);
                        if (dbConfig == null) continue;
                        IDbDialect dialect = DbDialectFactory.CreateDialect(dbConfig);
                        DbConnection conn = dialect.CreateConnection();
                        conn.Open();
                        tableConnections[tg.Key] = (conn, dialect);
                    }

                    // 以 uploadId（副檔名前的檔名）查詢 EsFileTransferUpload 是否存在
                    string uploadId = Path.GetFileNameWithoutExtension(filePath);
                    Guid? resolvedUploadId = null;
                    if (Guid.TryParse(uploadId, out Guid parsedUploadId) && QueryBomUploadExists(parsedUploadId))
                        resolvedUploadId = parsedUploadId;

                    // bomfilecontent 特殊處理：若找不到對應上傳記錄則略過
                    Dictionary<(string, string), Guid> bomUploadIds = new();
                    HashSet<(string, string)> skippedBomGroups = new();

                    foreach (var tg in tableGroups)
                    {
                        if (!string.Equals(tg.Key.TargetTableName, "bomfilecontent", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (!resolvedUploadId.HasValue)
                        {
                            skippedBomGroups.Add(tg.Key);
                            continue;
                        }
                        bomUploadIds[tg.Key] = resolvedUploadId.Value;
                    }

                    for (int rowIdx = dataStartRow; rowIdx <= sheet.LastRowNum; rowIdx++)
                    {
                        IRow row = sheet.GetRow(rowIdx);
                        if (row == null || IsRowEmpty(row)) continue;

                        // 讀取此列的所有標題值
                        var rowData = ReadExcelRow(row, headerRow, headerMap);

                        foreach (var tableGroup in tableGroups)
                        {
                            var columns = tableGroup.ToList();
                            if (!ApplyRowFilter(rowData, columns, out string filterError))
                            {
                                if (!string.IsNullOrEmpty(filterError))
                                {
                                    result.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = filterError });
                                }
                                continue;
                            }

                            if (skippedBomGroups.Contains(tableGroup.Key))
                                continue;

                            if (!tableConnections.TryGetValue(tableGroup.Key, out (DbConnection conn, IDbDialect dialect) connTuple))
                                continue;

                            Dictionary<string, object> extraValues = null;
                            if (bomUploadIds.TryGetValue(tableGroup.Key, out Guid bomUploadId))
                            {
                                extraValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                extraValues["uploadid"] = bomUploadId;
                            }

                            try
                            {
                                UpsertRow(rowData, columns, tableGroup.Key.TargetTableName, connTuple.conn, connTuple.dialect, extraValues);
                                result.DataCount++;
                            }
                            catch (Exception ex)
                            {
                                result.ErrorCount++;
                                result.ErrorLogs.Add(new EsTransferErrorLogDM
                                {
                                    Exception = $"第 {rowIdx + 1} 列寫入失敗：{ex.Message}"
                                });
                            }
                        }
                    }

                    // 轉檔結束後更新 EsFileTransferUpload 處理狀態
                    if (resolvedUploadId.HasValue)
                    {
                        bool hasBomTable = tableGroups.Any(tg =>
                            string.Equals(tg.Key.TargetTableName, "bomfilecontent", StringComparison.OrdinalIgnoreCase));
                        EsFileTransferUploadProcessStatusEnum newStatus = hasBomTable
                            ? EsFileTransferUploadProcessStatusEnum.PendingPartSearch
                            : EsFileTransferUploadProcessStatusEnum.Transferred;
                        EsFileTransferUploadDM? uploadDM = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>().GetOneInfoByUploadId(resolvedUploadId.Value);
                        if (uploadDM != null)
                            BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>().DoUpdateProcessStatus(uploadDM.Id, (int)newStatus);
                    }
                }
                finally
                {
                    foreach ((DbConnection conn, IDbDialect _) in tableConnections.Values)
                        try { conn.Dispose(); } catch { /* 釋放連線失敗不影響主流程 */ }
                }
            }
        }

        // ─── CSV 解析 ─────────────────────────────────────────────────

        // ─── 共用輔助：讀取一列 ──────────────────────────────────────

        private static Dictionary<string, string> ReadExcelRow(IRow row, IRow headerRow, Dictionary<string, int> headerMap)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in headerMap)
            {
                var cell = row.GetCell(kvp.Value);
                dict[kvp.Key] = cell == null ? null : GetCellValue(cell);
            }
            return dict;
        }

        private static Dictionary<string, int> BuildHeaderMap(IRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell == null) continue;
                var title = cell.ToString()?.Trim();
                if (!string.IsNullOrEmpty(title) && !map.ContainsKey(title))
                    map[title] = i;
            }
            return map;
        }

        private static string GetCellValue(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss")
                    : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                        ? cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss")
                        : cell.NumericCellValue.ToString(),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => cell.StringCellValue
                },
                _ => cell.ToString()?.Trim()
            };
        }

        private static bool IsRowEmpty(IRow row)
        {
            for (int i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                var cell = row.GetCell(i);
                if (cell != null && cell.CellType != CellType.Blank && !string.IsNullOrWhiteSpace(cell.ToString()))
                    return false;
            }
            return true;
        }

        // ─── CSV 解析輔助 ────────────────────────────────────────────

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        // ─── 篩選條件 ────────────────────────────────────────────────

        /// <summary>
        /// 取欄位群組中第一個有設定 FilterCondition 的欄位進行列篩選。
        /// 回傳 true 表示此列應寫入，false 表示略過。
        /// <para>
        /// FilterCondition 支援兩種格式：<br/>
        /// ① JSON 格式（前端 builder/manual 產生）：{"mode":"builder"|"manual","sql":"...","rows":[...]}<br/>
        ///   - builder：從 rows 陣列評估條件<br/>
        ///   - manual ：從 sql 欄位取出 WHERE 片段評估<br/>
        /// ② 純字串 SQL WHERE 片段（舊格式相容）
        /// </para>
        /// </summary>
        private static bool ApplyRowFilter(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns,
            out string errorMessage)
        {
            errorMessage = null;
            var filterCol = columns.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.FilterCondition));
            if (filterCol == null) return true;

            try
            {
                var condition = filterCol.FilterCondition.Trim();

                // FilterCondition 為 JSON 格式時，優先讀取 JSON 內的 mode 欄位
                if (condition.StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(condition);
                    var root = doc.RootElement;

                    // 從 JSON 的 "mode" 欄位判斷，若無則 fallback 到 filterCol.FilterMode
                    var mode = (root.TryGetProperty("mode", out var modeEl)
                        ? modeEl.GetString()
                        : filterCol.FilterMode)?.ToLower();

                    switch (mode)
                    {
                        case "builder":
                            // rows 已包含在 condition 的 JSON 中，直接傳入
                            return EvaluateBuilderFilter(rowData, condition);

                        case "manual":
                            // 從 JSON 的 "sql" 欄位取出實際的 WHERE 片段
                            var sql = root.TryGetProperty("sql", out var sqlEl)
                                ? sqlEl.GetString()?.Trim()
                                : null;
                            if (string.IsNullOrWhiteSpace(sql)) return true;
                            return EvaluateManualFilter(rowData, sql);

                        default:
                            return EvaluateManualFilter(rowData, condition);
                    }
                }
                else
                {
                    // 舊格式：純 SQL WHERE 片段，依 FilterMode 屬性分派
                    switch (filterCol.FilterMode?.ToLower())
                    {
                        case "builder":
                            return EvaluateBuilderFilter(rowData, condition);
                        case "manual":
                        default:
                            return EvaluateManualFilter(rowData, condition);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"篩選條件解析失敗：{ex.Message}";
                return true; // 解析失敗時不略過資料
            }
        }

        /// <summary>
        /// Builder 模式：FilterCondition 為 JSON
        /// {"rows":[{"logic":"","col":"ColA","op":"=","val":"X"}]}
        /// </summary>
        private static bool EvaluateBuilderFilter(Dictionary<string, string> rowData, string filterJson)
        {
            using var doc = JsonDocument.Parse(filterJson);
            if (!doc.RootElement.TryGetProperty("rows", out var rowsEl)) return true;

            bool? result = null;
            foreach (var rowEl in rowsEl.EnumerateArray())
            {
                string logic = rowEl.TryGetProperty("logic", out var l) ? l.GetString() ?? "AND" : "AND";
                string col = rowEl.TryGetProperty("col", out var c) ? c.GetString() : null;
                string op = rowEl.TryGetProperty("op", out var o) ? o.GetString() : null;
                string val = rowEl.TryGetProperty("val", out var v) ? v.GetString() : null;

                if (string.IsNullOrWhiteSpace(col) || string.IsNullOrWhiteSpace(op)) continue;

                rowData.TryGetValue(col, out string cellValue);
                bool match = EvaluateCondition(cellValue, op, val);

                result = result == null
                    ? match
                    : logic.ToUpper() == "OR" ? result.Value || match : result.Value && match;
            }

            return result ?? true;
        }

        private static bool EvaluateCondition(string cellValue, string op, string val)
        {
            return op.ToUpper() switch
            {
                "=" => string.Equals(cellValue, val, StringComparison.OrdinalIgnoreCase),
                "!=" or "<>" => !string.Equals(cellValue, val, StringComparison.OrdinalIgnoreCase),
                "LIKE" => cellValue?.Contains(val, StringComparison.OrdinalIgnoreCase) == true,
                "NOT LIKE" => cellValue?.Contains(val, StringComparison.OrdinalIgnoreCase) != true,
                "STARTS WITH" => cellValue?.StartsWith(val, StringComparison.OrdinalIgnoreCase) == true,
                "ENDS WITH" => cellValue?.EndsWith(val, StringComparison.OrdinalIgnoreCase) == true,
                "IS NULL" => string.IsNullOrEmpty(cellValue),
                "IS NOT NULL" => !string.IsNullOrEmpty(cellValue),
                ">" => double.TryParse(cellValue, out double cv) && double.TryParse(val, out double vv) && cv > vv,
                ">=" => double.TryParse(cellValue, out double cv2) && double.TryParse(val, out double vv2) && cv2 >= vv2,
                "<" => double.TryParse(cellValue, out double cv3) && double.TryParse(val, out double vv3) && cv3 < vv3,
                "<=" => double.TryParse(cellValue, out double cv4) && double.TryParse(val, out double vv4) && cv4 <= vv4,
                "IN" => val?.Split(',').Select(v => v.Trim()).Any(v => string.Equals(v, cellValue, StringComparison.OrdinalIgnoreCase)) == true,
                "NOT IN" => val?.Split(',').Select(v => v.Trim()).All(v => !string.Equals(v, cellValue, StringComparison.OrdinalIgnoreCase)) != false,
                "BETWEEN" => EvaluateBetween(cellValue, val),
                _ => true
            };
        }

        private static bool EvaluateBetween(string cellValue, string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return true;
            var parts = val.Split(',');
            if (parts.Length != 2) return true;
            if (double.TryParse(cellValue, out double cv)
                && double.TryParse(parts[0].Trim(), out double lo)
                && double.TryParse(parts[1].Trim(), out double hi))
                return cv >= lo && cv <= hi;
            // 文字比較（日期字串等）
            var lo2 = parts[0].Trim();
            var hi2 = parts[1].Trim();
            return string.Compare(cellValue, lo2, StringComparison.OrdinalIgnoreCase) >= 0
                && string.Compare(cellValue, hi2, StringComparison.OrdinalIgnoreCase) <= 0;
        }

        /// <summary>
        /// Manual 模式：解析 SQL WHERE 片段，支援 AND/OR 多條件
        /// 支援運算子：=, !=, &lt;&gt;, &gt;, &gt;=, &lt;, &lt;=, LIKE, NOT LIKE, IS NULL, IS NOT NULL
        /// </summary>
        private static bool EvaluateManualFilter(Dictionary<string, string> rowData, string whereClause)
        {
            var trimmed = whereClause?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmed)) return true;

            // 以 AND / OR 分隔，捕捉中間的邏輯運算子
            var condTokens = Regex.Split(trimmed, @"\s+(?:AND|OR)\s+", RegexOptions.IgnoreCase);
            var logicOps = Regex.Matches(trimmed, @"\s+(AND|OR)\s+", RegexOptions.IgnoreCase)
                .Select(m => m.Groups[1].Value.ToUpper())
                .ToList();

            bool? result = null;
            for (int i = 0; i < condTokens.Length; i++)
            {
                bool match = EvaluateSingleManualCondition(rowData, condTokens[i].Trim());
                if (result == null)
                    result = match;
                else
                    result = logicOps[i - 1] == "OR" ? result.Value || match : result.Value && match;
            }

            return result ?? true;
        }

        /// <summary>
        /// 解析單一條件片段，例如：[Col] = 'Value'、Col >= 10、Col IS NULL
        /// </summary>
        private static bool EvaluateSingleManualCondition(Dictionary<string, string> rowData, string condition)
        {
            // IS NULL / IS NOT NULL
            var isNullMatch = Regex.Match(condition,
                @"^\[?(\w+)\]?\s+(IS\s+NOT\s+NULL|IS\s+NULL)$",
                RegexOptions.IgnoreCase);
            if (isNullMatch.Success)
            {
                var col = isNullMatch.Groups[1].Value;
                var op = Regex.Replace(isNullMatch.Groups[2].Value, @"\s+", " ").ToUpper().Trim();
                rowData.TryGetValue(col, out string cv);
                return EvaluateCondition(cv, op, null);
            }

            // NOT LIKE / LIKE
            var likeMatch = Regex.Match(condition,
                @"^\[?(\w+)\]?\s+(NOT\s+LIKE|LIKE)\s+'([^']*)'$",
                RegexOptions.IgnoreCase);
            if (likeMatch.Success)
            {
                var col = likeMatch.Groups[1].Value;
                var op = Regex.Replace(likeMatch.Groups[2].Value, @"\s+", " ").ToUpper().Trim();
                var val = likeMatch.Groups[3].Value;
                rowData.TryGetValue(col, out string cv);
                return EvaluateCondition(cv, op, val);
            }

            // =, !=, <>, >, >=, <, <=
            var opMatch = Regex.Match(condition,
                @"^\[?(\w+)\]?\s*(!=|<>|>=|<=|>|<|=)\s*'?([^']*?)'?$");
            if (opMatch.Success)
            {
                var col = opMatch.Groups[1].Value;
                var op = opMatch.Groups[2].Value;
                var val = opMatch.Groups[3].Value.Trim();
                rowData.TryGetValue(col, out string cv);
                return EvaluateCondition(cv, op, val);
            }

            return true; // 無法解析時不略過資料
        }

        // ─── DB Upsert ───────────────────────────────────────────────

        /// <summary>
        /// 使用已開啟的連線執行 Upsert（供 Sheet/CSV 迴圈共用連線呼叫）。
        /// extraValues 中的鍵值對會在欄位對應完成後直接寫入目標資料，可用於注入額外欄位（如 uploadid）。
        /// </summary>
        private void UpsertRow(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns,
            string targetTableName,
            DbConnection conn,
            IDbDialect dialect,
            Dictionary<string, object> extraValues = null)
        {
            using DbTransaction transaction = conn.BeginTransaction();
            try
            {
                Dictionary<string, object> targetData = BuildTargetData(rowData, columns);

                // 注入呼叫端傳入的額外欄位（如 bomfilecontent.uploadid）
                if (extraValues != null)
                {
                    foreach (KeyValuePair<string, object> kvp in extraValues)
                        targetData[kvp.Key] = kvp.Value;
                }

                List<EsFileTransferMappingColumnDM> pkColumns = columns.Where(c => c.IsPrimaryKey).ToList();
                if (pkColumns.Count > 0)
                {
                    bool exists = CheckExists(conn, transaction, targetTableName, pkColumns, targetData, dialect);
                    if (exists)
                        UpdateRecord(conn, transaction, targetTableName, targetData, pkColumns, dialect);
                    else
                        InsertRecord(conn, transaction, targetTableName, targetData, dialect);
                }
                else
                {
                    InsertRecord(conn, transaction, targetTableName, targetData, dialect);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 將 rowData 依欄位設定（DefaultValue、加密）轉換為目標欄位值字典
        /// </summary>
        private Dictionary<string, object> BuildTargetData(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns)
        {
            var targetData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var secretKey = _config["SecretKey"];
            var secretIV = _config["SecretIV"];

            foreach (var col in columns)
            {
                rowData.TryGetValue(col.SrcFileColumnName, out string rawValue);

                // R03b: 值為 Null → 填入 DefaultValue
                if (string.IsNullOrEmpty(rawValue))
                    rawValue = col.DefaultValue;

                object finalValue = string.IsNullOrEmpty(rawValue) ? DBNull.Value : rawValue;

                // R07: IsEncrypt=true → 加密
                if (col.IsEncrypt && finalValue != DBNull.Value)
                {
                    if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(secretIV))
                        throw new InvalidOperationException("加密金鑰未設定");
                    finalValue = SecurityUtility.Encrypt(rawValue, secretKey, secretIV);
                }

                targetData[col.TargetTableColumnName] = finalValue;
            }

            return targetData;
        }

        private static bool CheckExists(
            DbConnection conn,
            DbTransaction tran,
            string tableName,
            List<EsFileTransferMappingColumnDM> pkColumns,
            Dictionary<string, object> targetData,
            IDbDialect dialect)
        {
            List<string> whereParts = new();
            using DbCommand cmd = conn.CreateCommand();
            cmd.Transaction = tran;

            for (int i = 0; i < pkColumns.Count; i++)
            {
                string pName = $"@pk{i}";
                whereParts.Add($"{dialect.QuoteIdentifier(pkColumns[i].TargetTableColumnName)} = {pName}");
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = pName;
                param.Value = targetData[pkColumns[i].TargetTableColumnName];
                cmd.Parameters.Add(param);
            }

            cmd.CommandText = $"SELECT COUNT(1) FROM {dialect.QuoteIdentifier(tableName)} WHERE {string.Join(" AND ", whereParts)}";
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void InsertRecord(
            DbConnection conn,
            DbTransaction tran,
            string tableName,
            Dictionary<string, object> targetData,
            IDbDialect dialect)
        {
            IEnumerable<string> cols = targetData.Keys.Select(k => dialect.QuoteIdentifier(k));
            IEnumerable<string> pNames = targetData.Keys.Select((k, i) => $"@p{i}");

            using DbCommand cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = $"INSERT INTO {dialect.QuoteIdentifier(tableName)} ({string.Join(",", cols)}) VALUES ({string.Join(",", pNames)})";

            int idx = 0;
            foreach (KeyValuePair<string, object> kvp in targetData)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = $"@p{idx++}";
                param.Value = kvp.Value;
                cmd.Parameters.Add(param);
            }

            cmd.ExecuteNonQuery();
        }

        private static void UpdateRecord(
            DbConnection conn,
            DbTransaction tran,
            string tableName,
            Dictionary<string, object> targetData,
            List<EsFileTransferMappingColumnDM> pkColumns,
            IDbDialect dialect)
        {
            HashSet<string> pkNames = pkColumns.Select(c => c.TargetTableColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<KeyValuePair<string, object>> updateCols = targetData.Where(kv => !pkNames.Contains(kv.Key)).ToList();
            if (updateCols.Count == 0) return;

            List<string> setClauses = updateCols.Select((kv, i) => $"{dialect.QuoteIdentifier(kv.Key)} = @set{i}").ToList();
            List<string> whereClauses = pkColumns.Select((c, i) => $"{dialect.QuoteIdentifier(c.TargetTableColumnName)} = @where{i}").ToList();

            using DbCommand cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = $"UPDATE {dialect.QuoteIdentifier(tableName)} SET {string.Join(",", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

            for (int i = 0; i < updateCols.Count; i++)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = $"@set{i}";
                param.Value = updateCols[i].Value;
                cmd.Parameters.Add(param);
            }
            for (int i = 0; i < pkColumns.Count; i++)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = $"@where{i}";
                param.Value = targetData[pkColumns[i].TargetTableColumnName];
                cmd.Parameters.Add(param);
            }

            cmd.ExecuteNonQuery();
        }

        // ─── 連線輔助 ────────────────────────────────────────────────

        private ESDbTransferDM GetOrCacheDbConfig(string dbTransferMappingCode)
        {
            if (_dbConfigCache.TryGetValue(dbTransferMappingCode, out var cached)) return cached;
            var bl = BLFactory.GetInstanceBackGround<TableExcelBL>();
            var cfg = bl.GetDbTransferConfig(dbTransferMappingCode);
            if (cfg != null) _dbConfigCache[dbTransferMappingCode] = cfg;
            return cfg;
        }

        /// <summary>
        /// 查詢 EsFileTransferUpload 是否存在指定 UploadId 的記錄。
        /// 用於 bomfilecontent 轉入前驗證對應的上傳記錄是否存在。
        /// </summary>
        private static bool QueryBomUploadExists(Guid uploadId)
        {
            EsFileTransferUploadBL bl = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>();
            return bl.GetOneInfoByUploadId(uploadId) != null;
        }

        // ─── 靜態工廠方法 ────────────────────────────────────────────

        private static EsScheduleCycleLogDetailDM ErrorResult(string transferCode, string message) =>
            new EsScheduleCycleLogDetailDM
            {
                FileTransferCode = transferCode,
                RunAt = DateTime.Now,
                //ErrorCount = 1,
                JobStatus = "Failed",
                ErrorLogs = { new EsTransferErrorLogDM { Exception = message } }
            };
    }

    /// <summary>
    /// 本地檔案轉入資料庫
    /// </summary>
    public partial class TransferJob
    {
        /// <summary>
        /// 從本地上傳目錄讀取符合設定副檔名的檔案，逐一執行轉入，成功後將檔案移至完成資料夾。
        /// </summary>
        private Task<EsScheduleCycleLogDetailDM> RunLocalTransfer()
        {
            EsScheduleCycleLogDetailDM logDM = new();
            logDM.FileTransferCode = _mapping.TransferMappingCode;
            logDM.RunAt = DateTime.Now;

            try
            {
                string uploadDir = BuildLocalUploadPath();
                string completeDir = BuildLocalCompletePath();

                if (!Directory.Exists(uploadDir))
                    return Task.FromResult(FailedResult(logDM, $"本地上傳目錄不存在：{uploadDir}"));

                string allowedExt = GetAllowedExtension(_mapping.ExampleFileType);
                List<string> matchedFiles = Directory.GetFiles(uploadDir)
                    .Where(f => string.Equals(Path.GetExtension(f), allowedExt, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedFiles.Count == 0)
                {
                    logDM.JobStatus = "Success";
                    return Task.FromResult(logDM);
                }

                List<string> successPaths = ProcessLocalFiles(matchedFiles, logDM);
                MoveLocalSuccessFiles(successPaths, completeDir);

                logDM.JobStatus = ResolveJobStatus(logDM.DataCount, logDM.ErrorCount);
            }
            catch (Exception ex)
            {
                logDM.JobStatus = "Failed";
                logDM.ErrorMessage = ex.Message;
            }
            finally
            {
                logDM.DurationMs = (int)(DateTime.Now - logDM.RunAt).TotalMilliseconds;
            }

            return Task.FromResult(logDM);
        }

        /// <summary>
        /// 逐一處理本地檔案並執行轉入，彙總結果至 logDM。
        /// 回傳全部轉入成功的本地檔案完整路徑清單。
        /// </summary>
        private List<string> ProcessLocalFiles(
            List<string> files,
            EsScheduleCycleLogDetailDM logDM)
        {
            List<string> successPaths = new();

            foreach (string filePath in files)
            {
                try
                {
                    EsScheduleCycleLogDetailDM transferResult = Execute(filePath);
                    logDM.DataCount += transferResult.DataCount;
                    logDM.ErrorCount += transferResult.ErrorCount;
                    logDM.ErrorLogs.AddRange(transferResult.ErrorLogs);

                    if (transferResult.ErrorCount == 0)
                        successPaths.Add(filePath);
                }
                catch (Exception ex)
                {
                    logDM.ErrorCount++;
                    logDM.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = ex.Message });
                }
            }

            return successPaths;
        }

        /// <summary>
        /// 將轉入成功的本地檔案逐一移動至完成資料夾。
        /// 移動失敗不影響整體結果。
        /// </summary>
        private static void MoveLocalSuccessFiles(
            List<string> successPaths,
            string completeDir)
        {
            if (!Directory.Exists(completeDir))
                Directory.CreateDirectory(completeDir);

            foreach (string srcPath in successPaths)
            {
                try
                {
                    MoveLocalFile(srcPath, completeDir);
                }
                catch
                {
                    // 移動失敗不影響整體結果，繼續處理其他檔案
                }
            }
        }

        /// <summary>
        /// 將單一本地檔案移動至目標資料夾。
        /// 若目標已存在同名檔案，先刪除再移動。
        /// </summary>
        private static void MoveLocalFile(
            string srcFilePath,
            string completeDir)
        {
            string fileName = Path.GetFileName(srcFilePath);
            string destFilePath = Path.Combine(completeDir, fileName);

            if (File.Exists(destFilePath))
                File.Delete(destFilePath);

            File.Move(srcFilePath, destFilePath);
        }

        /// <summary>
        /// 建立本地上傳目錄的完整路徑（對應 FileDirectoryConst.EsFileTransferUpload）。
        /// </summary>
        private string BuildLocalUploadPath()
        {
            string relativePath = Const.FileDirectoryConst.EsFileTransferUpload
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_webHostEnvironment.ContentRootPath, relativePath);
        }

        /// <summary>
        /// 建立本地轉入完成目錄的完整路徑（對應 FileDirectoryConst.EsFileTransferUploadComplete）。
        /// </summary>
        private string BuildLocalCompletePath()
        {
            string relativePath = Const.FileDirectoryConst.EsFileTransferUploadComplete
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_webHostEnvironment.ContentRootPath, relativePath);
        }
    }

    /// <summary>
    /// 其他排程
    /// </summary>
    public partial class TransferJob
    {
        /// <summary>
        /// 其他排程：根據 ActionType 執行對應排程工作
        /// </summary>
        /// <param name="actionType">排程動作類型（對應 ScheduleCycleActionTypeEnum）</param>
        /// <returns>執行結果</returns>
        public async Task<EsScheduleCycleLogDetailDM> OtherTransfer(int actionType)
        {
            DateTime st = DateTime.Now;

            EsScheduleCycleLogDetailDM logDM = new();
            logDM.OtherActionType = actionType;
            logDM.DataCount = 0;
            logDM.RunAt = st;

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                switch ((ScheduleCycleActionTypeEnum)actionType)
                {
                    case ScheduleCycleActionTypeEnum.ExtractKeyword:
                        ExtractKeywordJob extractKeywordJob = scope.ServiceProvider.GetRequiredService<ExtractKeywordJob>();
                        await extractKeywordJob.ExecuteAsync(logDM);
                        break;
                    case ScheduleCycleActionTypeEnum.DesideSanderModuleItemNo:
                        DesideSanderModuleItemNoJob desideSanderModuleItemNoJob = scope.ServiceProvider.GetRequiredService<DesideSanderModuleItemNoJob>();
                        await desideSanderModuleItemNoJob.ExecuteAsync(logDM);
                        break;
                    default:
                        logDM.JobStatus = "Failed";
                        logDM.ErrorMessage = $"未支援的 ActionType：{actionType}";
                        break;
                }
            }
            catch (Exception ex)
            {
                logDM.JobStatus = "Failed";
                logDM.ErrorMessage = ex.Message;
            }
            finally
            {
                logDM.DurationMs = (int)(DateTime.Now - st).TotalMilliseconds;
            }

            return logDM;
        }
    }
}
