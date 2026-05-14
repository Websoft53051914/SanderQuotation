using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Helper.DB;
using Core.Utility.Utility;
using DocumentFormat.OpenXml.InkML;
using backend.Common;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static Const.Enums;
using Const;

namespace backend.MESSource
{

    public partial class TransferJob
    {
        private IConfiguration _config;

        private SynoNasHelper _nasHelper;

        private EsFileTransferMappingDM _mapping;
        private readonly Dictionary<string, ESDbTransferDM> _dbConfigCache = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public TransferJob(IConfiguration config, IServiceScopeFactory scopeFactory, SynoNasHelper nasHelper)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            _nasHelper = nasHelper;
        }
        public async Task ExecuteTask(string scheduleCycleCode, string TriggerType)
        {
            EsScheduleCycleLogDM log = new();
            try
            {
                EsScheduleCycleBL bl = BLFactory.GetInstanceBackGround<EsScheduleCycleBL>();
                var data = bl.GetByCode(scheduleCycleCode);
                if (data == null || data.Status != StatusEnum.Enabled.ToValueString() || (data.DBTransferSettings.Count == 0 && data.FileTransferSettings.Count == 0 && data.DbCsvTransferSettings.Count == 0))
                {
                    // log not found
                    return;
                }

                DateTime st = DateTime.Now;

                log.RunAt = st;

                var dbTasks = data.DBTransferSettings.Select(setting => DBTransfer(setting));
                //var fileTasks = data.FileTransferSettings.Select(setting => FileTransfer(setting));
                //var dbCsvTasks = data.DbCsvTransferSettings.Select(setting => DBToCSVTransfer(setting));

                var dbResults = await Task.WhenAll(dbTasks);
                //var fileResults = await Task.WhenAll(fileTasks);
                //var dbCsvResults = await Task.WhenAll(dbCsvTasks);

                log.ScheduleCycleCode = scheduleCycleCode;
                log.DurationMs = (int)(DateTime.Now - st).TotalMilliseconds;
                //log.Details = dbResults.Concat(fileResults).Concat(dbCsvResults).ToList();
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
         

    }

    /// <summary>
    /// Nas檔案轉入資料庫
    /// </summary>
    public partial class TransferJob
    {
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
        /// 使用已開啟的連線執行 Upsert（供 Sheet/CSV 迴圈共用連線呼叫）
        /// </summary>
        private void UpsertRow(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns,
            string targetTableName,
            SqlConnection conn)
        {
            using var transaction = conn.BeginTransaction();
            try
            {
                var targetData = BuildTargetData(rowData, columns);

                var pkColumns = columns.Where(c => c.IsPrimaryKey).ToList();
                if (pkColumns.Count > 0)
                {
                    bool exists = CheckExists(conn, transaction, targetTableName, pkColumns, targetData);
                    if (exists)
                        UpdateRecord(conn, transaction, targetTableName, targetData, pkColumns);
                    else
                        InsertRecord(conn, transaction, targetTableName, targetData);
                }
                else
                {
                    InsertRecord(conn, transaction, targetTableName, targetData);
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

        private bool CheckExists(
            SqlConnection conn,
            SqlTransaction tran,
            string tableName,
            List<EsFileTransferMappingColumnDM> pkColumns,
            Dictionary<string, object> targetData)
        {
            var whereParts = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;

            for (int i = 0; i < pkColumns.Count; i++)
            {
                var pName = $"@pk{i}";
                whereParts.Add($"[{pkColumns[i].TargetTableColumnName}] = {pName}");
                cmd.Parameters.AddWithValue(pName, targetData[pkColumns[i].TargetTableColumnName]);
            }

            cmd.CommandText = $"SELECT COUNT(1) FROM [{tableName}] WHERE {string.Join(" AND ", whereParts)}";
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void InsertRecord(
            SqlConnection conn,
            SqlTransaction tran,
            string tableName,
            Dictionary<string, object> targetData)
        {
            var cols = targetData.Keys.Select(k => $"[{k}]");
            var pNames = targetData.Keys.Select((k, i) => $"@p{i}");

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = $"INSERT INTO [{tableName}] ({string.Join(",", cols)}) VALUES ({string.Join(",", pNames)})";

            int idx = 0;
            foreach (var kvp in targetData)
                cmd.Parameters.AddWithValue($"@p{idx++}", kvp.Value);

            cmd.ExecuteNonQuery();
        }

        private static void UpdateRecord(
            SqlConnection conn,
            SqlTransaction tran,
            string tableName,
            Dictionary<string, object> targetData,
            List<EsFileTransferMappingColumnDM> pkColumns)
        {
            var pkNames = pkColumns.Select(c => c.TargetTableColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var updateCols = targetData.Where(kv => !pkNames.Contains(kv.Key)).ToList();
            if (updateCols.Count == 0) return;

            var setClauses = updateCols.Select((kv, i) => $"[{kv.Key}] = @set{i}").ToList();
            var whereClauses = pkColumns.Select((c, i) => $"[{c.TargetTableColumnName}] = @where{i}").ToList();

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = $"UPDATE [{tableName}] SET {string.Join(",", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

            for (int i = 0; i < updateCols.Count; i++)
                cmd.Parameters.AddWithValue($"@set{i}", updateCols[i].Value);
            for (int i = 0; i < pkColumns.Count; i++)
                cmd.Parameters.AddWithValue($"@where{i}", targetData[pkColumns[i].TargetTableColumnName]);

            cmd.ExecuteNonQuery();
        }

        // ─── 連線輔助 ────────────────────────────────────────────────
         
        private static string BuildConnectionString(ESDbTransferDM dbConfig)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrWhiteSpace(dbConfig.DbPort)
                    ? dbConfig.DbHost
                    : $"{dbConfig.DbHost},{dbConfig.DbPort}",
                InitialCatalog = dbConfig.DbName,
                UserID = dbConfig.DbUser,
                Password = dbConfig.DbPassword,
                TrustServerCertificate = true
            };
            return builder.ConnectionString;
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


}
