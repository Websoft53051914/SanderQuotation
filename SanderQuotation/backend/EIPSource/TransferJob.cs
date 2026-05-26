using backend.Common.Attribute;
using backend.EIPSource;
using Business.BusinessLogic;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Utility;
using Hangfire;
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

        private EsFileTransferMappingDM _mapping;
        private readonly Dictionary<string, ESDbTransferDM> _dbConfigCache = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PathProvider _pathProvider;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="scopeFactory"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="pathProvider"></param>
        public TransferJob(IConfiguration config
            , IServiceScopeFactory scopeFactory
            , IWebHostEnvironment webHostEnvironment
            , PathProvider pathProvider)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            _webHostEnvironment = webHostEnvironment;
            _pathProvider = pathProvider;
        }

        [LogDeletedJob]
        [DisableConcurrentExecution(10)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task ExecuteTask(string scheduleCycleCode, string TriggerType)
        {
            EsScheduleCycleLogDM log = new();
            try
            {
                EsScheduleCycleBL bl = BLFactory.GetInstanceBackGround<EsScheduleCycleBL>();
                var data = bl.GetByCode(scheduleCycleCode);
                //Thread.Sleep(TimeSpan.FromSeconds(5));
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
        /// 檔案轉入：根據轉入代碼取得設定，讀取 EsFileTransferUpload 待轉資料並執行轉入。
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

            logDM = await RunLocalTransfer();

            return logDM;
        }
    }

    /// <summary>
    /// 檔案轉入資料庫
    /// </summary>
    public partial class TransferJob
    {
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
                    return ErrorResult(_mapping.TransferMappingCode, $"檔案不存在：{filePath}");

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

                    // 查詢各目標資料表的欄位型別（每張表只查一次，供型別轉換使用）
                    Dictionary<(string, string), Dictionary<string, string>> tableColumnTypes = new();
                    foreach (KeyValuePair<(string, string), (DbConnection conn, IDbDialect dialect)> tc in tableConnections)
                        tableColumnTypes[tc.Key] = tc.Value.dialect.GetColumnTypes(tc.Value.conn, tc.Key.Item1);

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
                                tableColumnTypes.TryGetValue(tableGroup.Key, out Dictionary<string, string>? colTypes);
                                UpsertRow(rowData, columns, tableGroup.Key.TargetTableName, connTuple.conn, connTuple.dialect, extraValues, colTypes);
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
        /// columnTypes 為目標資料表欄位型別字典，用於將字串值轉換為對應的 .NET 型別。
        /// </summary>
        private void UpsertRow(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns,
            string targetTableName,
            DbConnection conn,
            IDbDialect dialect,
            Dictionary<string, object> extraValues = null,
            Dictionary<string, string> columnTypes = null)
        {
            using DbTransaction transaction = conn.BeginTransaction();
            try
            {
                Dictionary<string, object> targetData = BuildTargetData(rowData, columns, columnTypes);

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
        /// 將 rowData 依欄位設定（DefaultValue、加密、型別轉換）轉換為目標欄位值字典
        /// </summary>
        private Dictionary<string, object> BuildTargetData(
            Dictionary<string, string> rowData,
            List<EsFileTransferMappingColumnDM> columns,
            Dictionary<string, string> columnTypes = null)
        {
            Dictionary<string, object> targetData = new(StringComparer.OrdinalIgnoreCase);
            var secretKey = _config["SecretKey"];
            var secretIV = _config["SecretIV"];

            foreach (EsFileTransferMappingColumnDM col in columns)
            {
                rowData.TryGetValue(col.SrcFileColumnName, out string rawValue);

                // R03b: 值為 Null → 填入 DefaultValue
                if (string.IsNullOrEmpty(rawValue))
                    rawValue = col.DefaultValue;

                object finalValue;
                if (string.IsNullOrEmpty(rawValue))
                {
                    finalValue = DBNull.Value;
                }
                else if (col.IsEncrypt)
                {
                    // R07: IsEncrypt=true → 加密（加密結果為字串，不需型別轉換）
                    if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(secretIV))
                        throw new InvalidOperationException("加密金鑰未設定");
                    finalValue = SecurityUtility.Encrypt(rawValue, secretKey, secretIV);
                }
                else
                {
                    // 依目標資料庫型別轉換字串值為對應 .NET 型別
                    string? dbType = null;
                    columnTypes?.TryGetValue(col.TargetTableColumnName, out dbType);
                    finalValue = CoerceToDbType(rawValue, dbType);
                }

                targetData[col.TargetTableColumnName] = finalValue;
            }

            return targetData;
        }

        /// <summary>
        /// 將字串值依目標資料庫欄位型別轉換為對應的 .NET 型別。
        /// dbType 為 null 或無法辨識時，保留原始字串。
        /// </summary>
        private static object CoerceToDbType(string rawValue, string? dbType)
        {
            if (string.IsNullOrEmpty(dbType))
                return rawValue;

            string t = dbType.ToLower();

            // 整數類型（MSSQL: int/bigint/smallint/tinyint；PostgreSQL: int4/int8/int2）
            if (t is "int" or "integer" or "bigint" or "smallint" or "tinyint" or "int4" or "int8" or "int2")
            {
                if (long.TryParse(rawValue, out long lv)) return lv;
                return DBNull.Value;
            }

            // 浮點/小數類型
            if (t is "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real"
                    or "double precision" or "float8" or "float4" or "double")
            {
                if (decimal.TryParse(rawValue, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal dv)) return dv;
                return DBNull.Value;
            }

            // 布林類型（MSSQL: bit；PostgreSQL: bool/boolean）
            if (t is "bit" or "bool" or "boolean")
            {
                if (rawValue == "1" || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (rawValue == "0" || rawValue.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
                return DBNull.Value;
            }

            // GUID 類型（MSSQL: uniqueidentifier；PostgreSQL: uuid）
            if (t is "uniqueidentifier" or "uuid")
            {
                if (Guid.TryParse(rawValue, out Guid gv)) return gv;
                return DBNull.Value;
            }

            // 日期時間類型
            if (t is "datetime" or "datetime2" or "date" or "time" or "smalldatetime"
                    or "timestamp" or "timestamp without time zone" or "timestamp with time zone"
                    or "timestamptz" or "timetz")
            {
                if (DateTime.TryParse(rawValue, out DateTime dtv)) return dtv;
                return DBNull.Value;
            }

            // 預設：字串
            return rawValue;
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
        /// 讀取 EsFileTransferUpload 中 ProcessStatus = Pending 且符合目前轉入規則的紀錄，逐一執行轉入。
        /// </summary>
        private Task<EsScheduleCycleLogDetailDM> RunLocalTransfer()
        {
            EsScheduleCycleLogDetailDM logDM = new();
            logDM.FileTransferCode = _mapping.TransferMappingCode;
            logDM.RunAt = DateTime.Now;

            try
            {
                SearchVO searchVO = new();
                searchVO.ProcessStatusEq = (int)EsFileTransferUploadProcessStatusEnum.Pending;
                searchVO.EsFileTransferMappingIdEq = _mapping.Id;
                List<EsFileTransferUploadDM> uploads = BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>()
                    .GetListEnabled(searchVO)
                    .ToList();

                if (uploads.Count == 0)
                {
                    logDM.JobStatus = "Success";
                    return Task.FromResult(logDM);
                }

                ProcessLocalFiles(uploads, logDM);

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
        /// 逐一取得對應本地檔案並執行轉入，彙總結果至 logDM。
        /// </summary>
        private void ProcessLocalFiles(
            List<EsFileTransferUploadDM> uploads,
            EsScheduleCycleLogDetailDM logDM)
        {
            string dirUpload = _pathProvider.EsFileTransferUpload;

            foreach (EsFileTransferUploadDM upload in uploads)
            {
                string filePath = Path.Combine(dirUpload, upload.UploadId.ToString() + Path.GetExtension(upload.FileName));
                try
                {
                    EsScheduleCycleLogDetailDM transferResult = Execute(filePath);
                    logDM.DataCount += transferResult.DataCount;
                    logDM.ErrorCount += transferResult.ErrorCount;
                    logDM.ErrorLogs.AddRange(transferResult.ErrorLogs);
                    if (transferResult.JobStatus == "Failed")
                        BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>().DoUpdateProcessStatus(upload.Id, (int)EsFileTransferUploadProcessStatusEnum.TransferredError);
                }
                catch (Exception ex)
                {
                    logDM.ErrorCount++;
                    logDM.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = ex.Message });
                    BLFactory.GetInstanceBackGround<EsFileTransferUploadBL>().DoUpdateProcessStatus(upload.Id, (int)EsFileTransferUploadProcessStatusEnum.TransferredError);
                }
            }
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
                    case ScheduleCycleActionTypeEnum.PartSearch:
                        DesideSanderModuleItemNoJob desideSanderModuleItemNoJob = scope.ServiceProvider.GetRequiredService<DesideSanderModuleItemNoJob>();
                        await desideSanderModuleItemNoJob.ExecuteAsync(logDM);
                        break;
                    case ScheduleCycleActionTypeEnum.PriceSearch:
                        PriceSearchJob priceSearchJob = scope.ServiceProvider.GetRequiredService<PriceSearchJob>();
                        await priceSearchJob.ExecuteAsync(logDM);
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
