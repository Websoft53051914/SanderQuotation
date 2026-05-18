using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Core.Utility.Utility;
using System.Data;
using System.Data.Common;

namespace Business.BusinessLogic
{
    /// <summary>
    /// ESDbTransferMapping 資料傳輸執行器
    /// 負責將資料從來源資料庫傳送至目的資料庫
    /// </summary>
    public class ESDbTransferExecutor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ESDbTransferMappingDM _mapping;
        private readonly List<ESDbTransferMappingColumnDM> _columns;
        private readonly ESDbTransferDM _srcDbConfig;
        private readonly ESDbTransferDM _dstDbConfig;
        private readonly string _secretKey;
        private readonly string _secretIV;
        private readonly IDbDialect _srcDialect;
        private readonly IDbDialect _dstDialect;

        public ESDbTransferExecutor(
            IUnitOfWork unitOfWork,
            ESDbTransferMappingDM mapping,
            List<ESDbTransferMappingColumnDM> columns,
            ESDbTransferDM srcDbConfig,
            ESDbTransferDM dstDbConfig,
            string secretKey = "",
            string secretIV = "")
        {
            _unitOfWork = unitOfWork;
            _mapping = mapping;
            _columns = columns ?? new List<ESDbTransferMappingColumnDM>();
            _srcDbConfig = srcDbConfig;
            _dstDbConfig = dstDbConfig;
            _secretKey = secretKey ?? string.Empty;
            _secretIV = secretIV ?? string.Empty;
            _srcDialect = DbDialectFactory.CreateDialect(srcDbConfig);
            _dstDialect = DbDialectFactory.CreateDialect(dstDbConfig);
        }

        /// <summary>
        /// 執行資料傳輸
        /// </summary>
        /// <returns>傳輸結果 <see cref="EsScheduleCycleLogDetailDM"/></returns>
        public EsScheduleCycleLogDetailDM Execute()
        {
            var result = new EsScheduleCycleLogDetailDM();
            try
            {
                // 驗證設定
                if (!ValidateConfiguration(out string validationError))
                {
                    result.ErrorCount = 1;
                    result.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = validationError });
                    return result;
                }

                // 建立來源資料庫連線
                using var srcConnection = _srcDialect.CreateConnection();
                srcConnection.Open();

                // 建立目的資料庫連線
                using var dstConnection = _dstDialect.CreateConnection();
                dstConnection.Open();

                // 從來源資料庫讀取資料
                var sourceData = FetchSourceData(srcConnection);

                if (sourceData == null || sourceData.Rows.Count == 0)
                    return result;

                // 處理每一筆資料
                foreach (DataRow row in sourceData.Rows)
                {
                    // 每筆資料使用獨立交易
                    using var transaction = dstConnection.BeginTransaction();
                    try
                    {
                        result.DataCount += 1;
                        ProcessRow(row, dstConnection, transaction);
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        result.ErrorCount++;
                        result.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.ErrorLogs.Add(new EsTransferErrorLogDM { Exception = ex.Message });
            }
            return result;
        }

        /// <summary>
        /// 驗證設定是否完整
        /// </summary>
        private bool ValidateConfiguration(out string errorMessage)
        {
            if (_mapping == null)
            {
                errorMessage = "傳輸對應設定不可為空";
                return false;
            }

            if (_columns == null || _columns.Count == 0)
            {
                errorMessage = "欄位對應設定不可為空";
                return false;
            }

            if (_srcDbConfig == null)
            {
                errorMessage = "來源資料庫設定不可為空";
                return false;
            }

            if (_dstDbConfig == null)
            {
                errorMessage = "目的資料庫設定不可為空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_mapping.SrcTableName))
            {
                errorMessage = "來源資料表名稱不可為空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_mapping.DstTableName))
            {
                errorMessage = "目的資料表名稱不可為空";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 從來源資料庫讀取資料
        /// </summary>
        private DataTable FetchSourceData(DbConnection connection)
        {
            string query = BuildSourceQuery();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = query;

            DataTable dataTable = new DataTable();
            using DbDataReader reader = command.ExecuteReader();
            dataTable.Load(reader);

            return dataTable;
        }

        /// <summary>
        /// 建立來源查詢SQL
        /// </summary>
        private string BuildSourceQuery()
        {
            IEnumerable<string> columnNames = _columns.Select(c => _srcDialect.QuoteIdentifier(c.SrcColumnName));
            string query = $"SELECT {string.Join(", ", columnNames)} FROM {_srcDialect.QuoteIdentifier(_mapping.SrcTableName)}";

            // 處理過濾條件
            string whereClause = BuildWhereClause();
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                query += $" WHERE {whereClause}";
            }

            return query;
        }

        /// <summary>
        /// 根據 FilterMode 建立 WHERE 子句
        /// </summary>
        private string BuildWhereClause()
        {
            // 如果沒有設定過濾條件，回傳空字串
            if (string.IsNullOrWhiteSpace(_mapping.FilterCondition))
            {
                return string.Empty;
            }

            // 判斷 FilterMode
            if (string.IsNullOrWhiteSpace(_mapping.FilterMode))
            {
                // 如果沒有設定 FilterMode，預設為 manual
                return _mapping.FilterCondition;
            }

            switch (_mapping.FilterMode.ToLower())
            {
                case "manual":
                    // 手寫 SQL 模式：直接使用 FilterCondition
                    return _mapping.FilterCondition;

                case "builder":
                    // 條件建構器模式：解析 JSON 並建立 WHERE 子句
                    return BuildWhereClauseFromJson(_mapping.FilterCondition);

                default:
                    // 未知模式，預設為 manual
                    return _mapping.FilterCondition;
            }
        }

        /// <summary>
        /// 從 JSON 格式的條件陣列建立 WHERE 子句
        /// </summary>
        /// <param name="jsonCondition">JSON 格式的條件陣列</param>
        /// <returns>WHERE 子句</returns>
        private string BuildWhereClauseFromJson(string jsonCondition)
        {
            try
            {
                // 解析 JSON（格式：{"mode":"builder","sql":"","rows":[{"logic":"","col":"SourceA","op":"=","val":"A1"}]}）
                var filterConfig = System.Text.Json.JsonSerializer.Deserialize<FilterConfiguration>(
                    jsonCondition,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (filterConfig == null || filterConfig.Rows == null || filterConfig.Rows.Count == 0)
                {
                    return string.Empty;
                }

                var whereClauses = new List<string>();

                for (int i = 0; i < filterConfig.Rows.Count; i++)
                {
                    var row = filterConfig.Rows[i];

                    if (string.IsNullOrWhiteSpace(row.Col) || string.IsNullOrWhiteSpace(row.Op))
                    {
                        continue;
                    }

                    var clause = BuildSingleCondition(row);
                    if (!string.IsNullOrWhiteSpace(clause))
                    {
                        // 第一個條件不加邏輯運算子，後續條件根據 logic 決定
                        if (i == 0)
                        {
                            whereClauses.Add(clause);
                        }
                        else
                        {
                            var logic = string.IsNullOrWhiteSpace(row.Logic) ? "AND" : row.Logic.ToUpper();
                            whereClauses.Add($"{logic} {clause}");
                        }
                    }
                }

                return whereClauses.Count > 0 ? string.Join(" ", whereClauses) : string.Empty;
            }
            catch (Exception ex)
            {
                // JSON 解析失敗，記錄錯誤並回傳空字串
                Console.WriteLine($"解析過濾條件 JSON 時發生錯誤: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 建立單一條件子句
        /// </summary>
        private string BuildSingleCondition(FilterRow row)
        {
            string field = _srcDialect.QuoteIdentifier(row.Col);
            string op = row.Op?.ToUpper();
            string value = row.Val;

            // 處理不同的運算子
            switch (op)
            {
                case "=":
                case "!=":
                case ">":
                case ">=":
                case "<":
                case "<=":
                case "<>":
                    // 判斷值的類型，加上適當的引號
                    if (IsNumeric(value))
                    {
                        return $"{field} {row.Op} {value}";
                    }
                    else
                    {
                        return $"{field} {row.Op} '{EscapeSqlString(value)}'";
                    }

                case "LIKE":
                    return $"{field} LIKE '%{EscapeSqlString(value)}%'";

                case "NOT LIKE":
                    return $"{field} NOT LIKE '%{EscapeSqlString(value)}%'";

                case "STARTS WITH":
                    return $"{field} LIKE '{EscapeSqlString(value)}%'";

                case "ENDS WITH":
                    return $"{field} LIKE '%{EscapeSqlString(value)}'";

                case "IN":
                    // value 應該是逗號分隔的值
                    var values = value?.Split(',').Select(v => $"'{EscapeSqlString(v.Trim())}'");
                    return $"{field} IN ({string.Join(",", values ?? Array.Empty<string>())})";

                case "NOT IN":
                    var notInValues = value?.Split(',').Select(v => $"'{EscapeSqlString(v.Trim())}'");
                    return $"{field} NOT IN ({string.Join(",", notInValues ?? Array.Empty<string>())})";

                case "IS NULL":
                    return $"{field} IS NULL";

                case "IS NOT NULL":
                    return $"{field} IS NOT NULL";

                case "BETWEEN":
                    // value 格式：value1,value2
                    var betweenValues = value?.Split(',');
                    if (betweenValues?.Length == 2)
                    {
                        return $"{field} BETWEEN '{EscapeSqlString(betweenValues[0].Trim())}' AND '{EscapeSqlString(betweenValues[1].Trim())}'";
                    }
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 判斷字串是否為數字
        /// </summary>
        private bool IsNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return double.TryParse(value, out _);
        }

        /// <summary>
        /// 跳脫 SQL 字串中的單引號，防止 SQL Injection
        /// </summary>
        private string EscapeSqlString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace("'", "''");
        }

        /// <summary>
        /// 過濾條件設定
        /// </summary>
        private class FilterConfiguration
        {
            public string Mode { get; set; }
            public string Sql { get; set; }
            public List<FilterRow> Rows { get; set; }
        }

        /// <summary>
        /// 過濾條件列
        /// </summary>
        private class FilterRow
        {
            public string Logic { get; set; }  // AND / OR
            public string Col { get; set; }    // 欄位名稱
            public string Op { get; set; }     // 運算子
            public string Val { get; set; }    // 值
        }


        /// <summary>
        /// 處理單筆資料
        /// </summary>
        private void ProcessRow(DataRow sourceRow, DbConnection dstConnection, DbTransaction transaction)
        {
            // 準備目的資料
            var targetData = new Dictionary<string, object>();
            var primaryKeyColumns = _columns.Where(c => c.IsPrimaryKey).ToList();

            foreach (var column in _columns)
            {
                var sourceValue = sourceRow[column.SrcColumnName];

                // 處理 NULL 值
                if (sourceValue == DBNull.Value || sourceValue == null)
                {
                    targetData[column.DstColumnName] = DBNull.Value;
                    continue;
                }

                // 如果需要加密
                if (column.IsEncrypt)
                {
                    if (string.IsNullOrEmpty(_secretKey) || string.IsNullOrEmpty(_secretIV))
                    {
                        throw new InvalidOperationException("加密金鑰或IV未設定");
                    }
                    var encryptedValue = SecurityUtility.Encrypt(sourceValue.ToString(), _secretKey, _secretIV);
                    targetData[column.DstColumnName] = encryptedValue;
                }
                else
                {
                    targetData[column.DstColumnName] = sourceValue;
                }
            }

            // 判斷是否需要 UPDATE 或 INSERT
            if (primaryKeyColumns.Count > 0)
            {
                var exists = CheckRecordExists(dstConnection, primaryKeyColumns, targetData, transaction);
                if (exists)
                {
                    UpdateRecord(dstConnection, targetData, primaryKeyColumns, transaction);
                }
                else
                {
                    InsertRecord(dstConnection, targetData, transaction);
                }
            }
            else
            {
                // 沒有主鍵設定，直接 INSERT
                InsertRecord(dstConnection, targetData, transaction);
            }
        }

        /// <summary>
        /// 檢查記錄是否存在
        /// </summary>
        private bool CheckRecordExists(
            DbConnection connection,
            List<ESDbTransferMappingColumnDM> primaryKeyColumns,
            Dictionary<string, object> targetData,
            DbTransaction transaction)
        {
            List<string> whereConditions = new();
            DbCommand command = connection.CreateCommand();
            command.Transaction = transaction;

            for (int i = 0; i < primaryKeyColumns.Count; i++)
            {
                ESDbTransferMappingColumnDM pkColumn = primaryKeyColumns[i];
                string paramName = $"@pk{i}";
                whereConditions.Add($"{_dstDialect.QuoteIdentifier(pkColumn.DstColumnName)} = {paramName}");

                DbParameter param = command.CreateParameter();
                param.ParameterName = paramName;
                param.Value = targetData[pkColumn.DstColumnName] ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            command.CommandText = $"SELECT COUNT(*) FROM {_dstDialect.QuoteIdentifier(_mapping.DstTableName)} WHERE {string.Join(" AND ", whereConditions)}";

            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        /// <summary>
        /// 新增記錄
        /// </summary>
        private void InsertRecord(DbConnection connection, Dictionary<string, object> targetData, DbTransaction transaction)
        {
            IEnumerable<string> columnNames = targetData.Keys.Select(k => _dstDialect.QuoteIdentifier(k));
            IEnumerable<string> paramNames = targetData.Keys.Select((k, i) => $"@p{i}");

            string insertSql = $"INSERT INTO {_dstDialect.QuoteIdentifier(_mapping.DstTableName)} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";

            using DbCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = insertSql;

            int paramIndex = 0;
            foreach (KeyValuePair<string, object> kvp in targetData)
            {
                DbParameter param = command.CreateParameter();
                param.ParameterName = $"@p{paramIndex}";
                param.Value = kvp.Value ?? DBNull.Value;
                command.Parameters.Add(param);
                paramIndex++;
            }

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 更新記錄
        /// </summary>
        private void UpdateRecord(
            DbConnection connection,
            Dictionary<string, object> targetData,
            List<ESDbTransferMappingColumnDM> primaryKeyColumns,
            DbTransaction transaction)
        {
            List<string> primaryKeyNames = primaryKeyColumns.Select(c => c.DstColumnName).ToList();
            List<KeyValuePair<string, object>> updateColumns = targetData.Where(kvp => !primaryKeyNames.Contains(kvp.Key)).ToList();

            if (updateColumns.Count == 0)
            {
                // 沒有需要更新的欄位(全部都是主鍵)
                return;
            }

            List<string> setClause = new();
            List<string> whereClause = new();
            DbCommand command = connection.CreateCommand();
            command.Transaction = transaction;

            // 建立 SET 子句
            for (int i = 0; i < updateColumns.Count; i++)
            {
                KeyValuePair<string, object> kvp = updateColumns[i];
                string paramName = $"@set{i}";
                setClause.Add($"{_dstDialect.QuoteIdentifier(kvp.Key)} = {paramName}");

                DbParameter param = command.CreateParameter();
                param.ParameterName = paramName;
                param.Value = kvp.Value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            // 建立 WHERE 子句
            for (int i = 0; i < primaryKeyColumns.Count; i++)
            {
                ESDbTransferMappingColumnDM pkColumn = primaryKeyColumns[i];
                string paramName = $"@where{i}";
                whereClause.Add($"{_dstDialect.QuoteIdentifier(pkColumn.DstColumnName)} = {paramName}");

                DbParameter param = command.CreateParameter();
                param.ParameterName = paramName;
                param.Value = targetData[pkColumn.DstColumnName] ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            command.CommandText = $"UPDATE {_dstDialect.QuoteIdentifier(_mapping.DstTableName)} SET {string.Join(", ", setClause)} WHERE {string.Join(" AND ", whereClause)}";

            command.ExecuteNonQuery();
        }

    }
}

