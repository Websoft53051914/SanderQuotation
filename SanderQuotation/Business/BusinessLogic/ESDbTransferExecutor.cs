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

                // 查詢來源資料表欄位型別，供 WHERE 條件參數型別轉換使用
                Dictionary<string, string> srcColumnTypes = _srcDialect.GetColumnTypes(srcConnection, _mapping.SrcTableName);

                // 建立目的資料庫連線
                using var dstConnection = _dstDialect.CreateConnection();
                dstConnection.Open();

                // 查詢目的資料表欄位型別，供型別轉換使用
                Dictionary<string, string> dstColumnTypes = _dstDialect.GetColumnTypes(dstConnection, _mapping.DstTableName);

                // 從來源資料庫讀取資料
                var sourceData = FetchSourceData(srcConnection, srcColumnTypes);

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
                        ProcessRow(row, dstConnection, transaction, dstColumnTypes);
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
        private DataTable FetchSourceData(DbConnection connection, Dictionary<string, string> srcColumnTypes)
        {
            (string query, List<(string Name, object Value)> filterParams) = BuildSourceQuery(srcColumnTypes);
            using DbCommand command = connection.CreateCommand();
            command.CommandText = query;

            foreach ((string name, object value) in filterParams)
            {
                DbParameter param = command.CreateParameter();
                param.ParameterName = name;
                param.Value = value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            DataTable dataTable = new DataTable();
            using DbDataReader reader = command.ExecuteReader();
            dataTable.Load(reader);

            return dataTable;
        }

        /// <summary>
        /// 建立來源查詢 SQL（含參數化過濾條件）
        /// </summary>
        private (string Sql, List<(string Name, object Value)> Params) BuildSourceQuery(Dictionary<string, string> srcColumnTypes)
        {
            IEnumerable<string> columnNames = _columns.Select(c => _srcDialect.QuoteIdentifier(c.SrcColumnName));
            string sql = $"SELECT {string.Join(", ", columnNames)} FROM {_srcDialect.QuoteIdentifier(_mapping.SrcTableName)}";

            (string whereClause, List<(string Name, object Value)> filterParams) = BuildWhereClause(srcColumnTypes);
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql += $" WHERE {whereClause}";

            return (sql, filterParams);
        }

        /// <summary>
        /// 根據 FilterMode 建立 WHERE 子句（builder 模式回傳參數清單）
        /// </summary>
        private (string WhereClause, List<(string Name, object Value)> Params) BuildWhereClause(Dictionary<string, string> srcColumnTypes)
        {
            if (string.IsNullOrWhiteSpace(_mapping.FilterCondition))
                return (string.Empty, new List<(string, object)>());

            if (string.IsNullOrWhiteSpace(_mapping.FilterMode))
                return (_mapping.FilterCondition, new List<(string, object)>());

            switch (_mapping.FilterMode.ToLower())
            {
                case "builder":
                    return BuildWhereClauseFromJson(_mapping.FilterCondition, srcColumnTypes);

                case "manual":
                default:
                    // 手寫 SQL：直接使用，無額外參數
                    return (_mapping.FilterCondition, new List<(string, object)>());
            }
        }

        /// <summary>
        /// 從 JSON 格式的條件陣列建立參數化 WHERE 子句
        /// </summary>
        private (string WhereClause, List<(string Name, object Value)> Params) BuildWhereClauseFromJson(string jsonCondition, Dictionary<string, string> srcColumnTypes)
        {
            List<(string Name, object Value)> parameters = new();
            try
            {
                FilterConfiguration filterConfig = System.Text.Json.JsonSerializer.Deserialize<FilterConfiguration>(
                    jsonCondition,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (filterConfig == null || filterConfig.Rows == null || filterConfig.Rows.Count == 0)
                    return (string.Empty, parameters);

                List<string> whereClauses = new();

                foreach (FilterRow row in filterConfig.Rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Col) || string.IsNullOrWhiteSpace(row.Op))
                        continue;

                    string clause = BuildSingleCondition(row, parameters, srcColumnTypes);
                    if (!string.IsNullOrWhiteSpace(clause))
                    {
                        // whereClauses 為空時為第一個條件，不加邏輯運算子
                        string prefix = whereClauses.Count == 0
                            ? string.Empty
                            : (string.IsNullOrWhiteSpace(row.Logic) ? "AND" : row.Logic.ToUpper()) + " ";
                        whereClauses.Add($"{prefix}{clause}");
                    }
                }

                return (string.Join(" ", whereClauses), parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析過濾條件 JSON 時發生錯誤: {ex.Message}");
                return (string.Empty, parameters);
            }
        }

        /// <summary>
        /// 建立單一參數化條件子句，並將參數加入 parameters 清單。
        /// 單值比較運算子（=、!=、&lt;&gt;、&gt;、&gt;=、&lt;、&lt;=）會依來源欄位型別套用 CoerceToDbType，
        /// 使 PostgreSQL 等強型態資料庫能正確做型態比對。
        /// LIKE、IN、BETWEEN 等以字串語意為主，保留字串型別。
        /// </summary>
        private string BuildSingleCondition(FilterRow row, List<(string Name, object Value)> parameters, Dictionary<string, string> srcColumnTypes)
        {
            string field = _srcDialect.QuoteIdentifier(row.Col);
            string op = row.Op?.ToUpper();
            string value = row.Val;
            int baseIdx = parameters.Count;

            switch (op)
            {
                case "=":
                case "!=":
                case "<>":
                case ">":
                case ">=":
                case "<":
                case "<=":
                {
                    string pName = $"@w{baseIdx}";
                    srcColumnTypes.TryGetValue(row.Col, out string srcDbType);
                    object coerced = string.IsNullOrEmpty(value)
                        ? DBNull.Value
                        : CoerceToDbType(value, srcDbType);
                    parameters.Add((pName, coerced));
                    return $"{field} {row.Op} {pName}";
                }

                case "LIKE":
                {
                    string pName = $"@w{baseIdx}";
                    parameters.Add((pName, $"%{value}%"));
                    return $"{field} LIKE {pName}";
                }

                case "NOT LIKE":
                {
                    string pName = $"@w{baseIdx}";
                    parameters.Add((pName, $"%{value}%"));
                    return $"{field} NOT LIKE {pName}";
                }

                case "STARTS WITH":
                {
                    string pName = $"@w{baseIdx}";
                    parameters.Add((pName, $"{value}%"));
                    return $"{field} LIKE {pName}";
                }

                case "ENDS WITH":
                {
                    string pName = $"@w{baseIdx}";
                    parameters.Add((pName, $"%{value}"));
                    return $"{field} LIKE {pName}";
                }

                case "IN":
                {
                    string[] vals = value?.Split(',') ?? Array.Empty<string>();
                    List<string> pNames = new();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        string pName = $"@w{baseIdx + i}";
                        parameters.Add((pName, vals[i].Trim()));
                        pNames.Add(pName);
                    }
                    return pNames.Count > 0 ? $"{field} IN ({string.Join(", ", pNames)})" : string.Empty;
                }

                case "NOT IN":
                {
                    string[] vals = value?.Split(',') ?? Array.Empty<string>();
                    List<string> pNames = new();
                    for (int i = 0; i < vals.Length; i++)
                    {
                        string pName = $"@w{baseIdx + i}";
                        parameters.Add((pName, vals[i].Trim()));
                        pNames.Add(pName);
                    }
                    return pNames.Count > 0 ? $"{field} NOT IN ({string.Join(", ", pNames)})" : string.Empty;
                }

                case "IS NULL":
                    return $"{field} IS NULL";

                case "IS NOT NULL":
                    return $"{field} IS NOT NULL";

                case "BETWEEN":
                {
                    string[] parts = value?.Split(',') ?? Array.Empty<string>();
                    if (parts.Length == 2)
                    {
                        string p1 = $"@w{baseIdx}";
                        string p2 = $"@w{baseIdx + 1}";
                        parameters.Add((p1, parts[0].Trim()));
                        parameters.Add((p2, parts[1].Trim()));
                        return $"{field} BETWEEN {p1} AND {p2}";
                    }
                    return string.Empty;
                }

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 將字串值依目標資料庫欄位型別轉換為對應的 .NET 型別。
        /// dbType 為 null 或無法辨識時，保留原始字串。
        /// </summary>
        private static object CoerceToDbType(string rawValue, string dbType)
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
        private void ProcessRow(DataRow sourceRow, DbConnection dstConnection, DbTransaction transaction, Dictionary<string, string> dstColumnTypes)
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
                    string encryptedValue = SecurityUtility.Encrypt(sourceValue.ToString(), _secretKey, _secretIV);
                    targetData[column.DstColumnName] = encryptedValue;
                }
                else
                {
                    // 依目的欄位型別做型別轉換
                    dstColumnTypes.TryGetValue(column.DstColumnName, out string dbType);
                    targetData[column.DstColumnName] = CoerceToDbType(sourceValue.ToString(), dbType);
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

