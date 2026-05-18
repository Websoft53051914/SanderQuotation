using Business.DomainModel;
using Const;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using static Const.Enums;

namespace Business.Common
{
    /// <summary>
    /// 資料庫方言介面，封裝各資料庫類型的連線與識別名稱引號差異。
    /// 新增資料庫類型時，實作此介面並在 <see cref="DbDialectFactory.CreateDialect"/> 加入對應 case。
    /// </summary>
    public interface IDbDialect
    {
        /// <summary>
        /// 建立尚未開啟的資料庫連線物件
        /// </summary>
        DbConnection CreateConnection();

        /// <summary>
        /// 回傳符合該資料庫語法的識別名稱引號格式
        /// </summary>
        string QuoteIdentifier(string name);
    }

    /// <summary>
    /// 根據資料庫設定建立對應 <see cref="IDbDialect"/> 實作的工廠類別。
    /// </summary>
    public static class DbDialectFactory
    {
        /// <summary>
        /// 根據 <see cref="ESDbTransferDM.DbType"/> 建立對應的 <see cref="IDbDialect"/> 實作。
        /// 新增資料庫類型時，於此 switch 加入對應 case 與實作類別。
        /// </summary>
        public static IDbDialect CreateDialect(ESDbTransferDM dbConfig)
        {
            ArgumentNullException.ThrowIfNull(dbConfig);

            string dbType = dbConfig.DbType?.Trim().ToUpper() ?? string.Empty;
            if (dbType == ((int)EsDbTransferDbTypeEnum.PostgreSQL).ToString())
            {
                return new PostgreSqlDialect(dbConfig);
            }
            else
            {
                return new MssqlDialect(dbConfig);
            }
        }
    }

    /// <summary>
    /// MSSQL 方言實作
    /// </summary>
    internal sealed class MssqlDialect : IDbDialect
    {
        private readonly ESDbTransferDM _config;

        /// <summary>
        /// 以指定的資料庫設定初始化 <see cref="MssqlDialect"/>。
        /// </summary>
        public MssqlDialect(ESDbTransferDM config)
        {
            _config = config;
        }

        /// <summary>
        /// 建立 SQL Server 連線
        /// </summary>
        public DbConnection CreateConnection()
        {
            SqlConnectionStringBuilder builder = new();
            builder.DataSource = string.IsNullOrWhiteSpace(_config.DbPort)
                ? _config.DbHost
                : $"{_config.DbHost},{_config.DbPort}";
            builder.InitialCatalog = _config.DbName;
            builder.UserID = _config.DbUser;
            builder.Password = _config.DbPassword;
            builder.TrustServerCertificate = true;
            return new SqlConnection(builder.ConnectionString);
        }

        /// <summary>
        /// 回傳 MSSQL 識別名稱引號格式（方括號）
        /// </summary>
        public string QuoteIdentifier(string name) => $"[{name}]";
    }

    /// <summary>
    /// PostgreSQL 方言實作
    /// </summary>
    internal sealed class PostgreSqlDialect : IDbDialect
    {
        private readonly ESDbTransferDM _config;

        /// <summary>
        /// 以指定的資料庫設定初始化 <see cref="PostgreSqlDialect"/>。
        /// </summary>
        public PostgreSqlDialect(ESDbTransferDM config)
        {
            _config = config;
        }

        /// <summary>
        /// 建立 PostgreSQL 連線
        /// </summary>
        public DbConnection CreateConnection()
        {
            NpgsqlConnectionStringBuilder pgBuilder = new();
            pgBuilder.Host = _config.DbHost;
            if (!string.IsNullOrWhiteSpace(_config.DbPort) && int.TryParse(_config.DbPort, out int port))
                pgBuilder.Port = port;
            pgBuilder.Database = _config.DbName;
            pgBuilder.Username = _config.DbUser;
            pgBuilder.Password = _config.DbPassword;
            return new NpgsqlConnection(pgBuilder.ConnectionString);
        }

        /// <summary>
        /// 回傳 PostgreSQL 識別名稱引號格式（雙引號）
        /// </summary>
        public string QuoteIdentifier(string name) => $"\"{name}\"";
    }
}
