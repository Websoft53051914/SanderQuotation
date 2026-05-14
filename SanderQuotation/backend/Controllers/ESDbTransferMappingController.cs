using AutoMapper;
using backend.Common;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using SixLabors.ImageSharp.ColorSpaces;
using System.Data;
using System.Data.SqlClient;
using ViewModel;
using static Const.Enums;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace backend.Controllers
{
    [Route("api/ESDbTransferMapping")]
    public class ESDbTransferMappingController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public ESDbTransferMappingController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ESDbTransferMappingVM, ESDbTransferMappingDM>().ReverseMap();
                cfg.CreateMap<ESDbTransferMappingColumnVM, ESDbTransferMappingColumnDM>().ReverseMap();

            });
            _mapper = mapperConfig.CreateMapper();
        }

        private ESDbTransferMappingBL? _bl;
        private ESDbTransferMappingBL GetBL()
        {
            _bl ??= GetBLInstance<ESDbTransferMappingBL>();
            return _bl;
        }

        private ESDbTransferBL? _transferBL;
        private ESDbTransferBL GetTransferBL()
        {
            _transferBL ??= GetBLInstance<ESDbTransferBL>();
            return _transferBL;
        }

        // ── CRUD ────────────────────────────────────────────────────────────────

        [HttpGet("GetPageList")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_View)]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, string Keyword)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                IMapper mapper = CommonUtility.CreateMapper<ESDbTransferMappingVM, ESDbTransferMappingDM>();
                SearchVO searchVO = new SearchVO()
                {
                    KeywordLike = Keyword
                };

                var pageResult = GetBL().GetPageList(pageEntity, searchVO);
                var list = _mapper.Map<List<ESDbTransferMappingVM>>(pageResult.Results);

                //取得所有資料庫設定

                var resDB = GetTransferBL().GetAll();
                var dicDb = resDB.ToDictionary(k => k.TransferCode, v => v.TransferName);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
                    if (string.IsNullOrEmpty(list[i].Description))
                        list[i].Description = "";

                    if (dicDb.ContainsKey(list[i].DstDbTransferCode))
                        list[i].DstTransferName = dicDb[list[i].DstDbTransferCode];

                    if (dicDb.ContainsKey(list[i].SrcDbTransferCode))
                        list[i].SrcTransferName = dicDb[list[i].SrcDbTransferCode];

                    list[i].Columns = _mapper.Map<List<ESDbTransferMappingColumnVM>>(GetBL().GetColumns(list[i].TransferMappingCode));

                    list[i].CanDelete = pageResult.Results[i].EsScheduleCycleDMs.Count == 0; // 若有排程綁定則不可刪除
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount,
                    Page = pageResult.CurrentPage,
                    PageSize = pageResult.PageDataSize
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Get")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Edit)]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetBL().Get(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<ESDbTransferMappingVM>(dm);
                vm.Columns = _mapper.Map<List<ESDbTransferMappingColumnVM>>(GetBL().GetColumns(vm.TransferMappingCode));

                var resDB = GetTransferBL().GetAll();
                var dicDb = resDB.ToDictionary(k => k.TransferCode, v => v.TransferName);

                if (dicDb.ContainsKey(vm.DstDbTransferCode))
                    vm.DstTransferName = dicDb[vm.DstDbTransferCode];

                if (dicDb.ContainsKey(vm.SrcDbTransferCode))
                    vm.SrcTransferName = dicDb[vm.SrcDbTransferCode];

                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Create")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create)]
        public IActionResult Create(ESDbTransferMappingVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferMappingDM>(vm);
                dm.Columns = vm.Columns.Select(c => new ESDbTransferMappingColumnDM
                {
                    SrcColumnName = c.SrcColumnName,
                    DstColumnName = c.DstColumnName,
                    IsEncrypt = c.IsEncrypt,
                    IsPrimaryKey = c.IsPrimaryKey
                }).ToList();
                GetBL().CheckExist(dm);
                if (GetBL().GetMessage().IsError())
                {
                    return JsonValidFail(GetBL().GetMessage().GetAlert());
                }
                var guid = GetBL().Create(dm);
                LogSuccess(guid, LogAction.Create);
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Edit")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Edit)]
        public IActionResult Edit(ESDbTransferMappingVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferMappingDM>(vm);
                GetBL().Edit(dm);
                LogSuccess(vm.Id, LogAction.Edit);
                return JsonSuccess("編輯成功");

            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Delete")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Delete)]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                GetBL().Delete(list);
                foreach (var item in list)
                {
                    LogSuccess(item, LogAction.Delete);
                }
                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        /// <summary>
        /// 取得指定主表的欄位對應明細
        /// </summary>
        [HttpGet("GetColumns")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create, FuncID.ESDbTransferMapping_Edit)]
        public IActionResult GetColumns([FromQuery] string TransferMappingCode)
        {
            try
            {
                var cols = GetBL().GetColumns(TransferMappingCode);
                var colVms = cols.Select(c => new ESDbTransferMappingColumnVM
                {
                    Id = c.Id,
                    TransferMappingCode = c.TransferMappingCode,
                    SrcColumnName = c.SrcColumnName,
                    DstColumnName = c.DstColumnName,
                    IsEncrypt = c.IsEncrypt,
                    IsPrimaryKey = c.IsPrimaryKey,
                    Status = c.Status.HasValue ? c.Status.Value.ToString() : "",
                }).ToList();
                return JsonSuccess(colVms);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 來源資料庫下拉清單 ───────────────────────────────────────────────────

        /// <summary>
        /// 取得所有已設定的來源資料庫連線清單（ESDbTransfer）
        /// </summary>
        [HttpGet("GetDbTransferList")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create, FuncID.ESDbTransferMapping_Edit, FuncID.TableExcel_Create, FuncID.TableExcel_Edit)]
        public IActionResult GetDbTransferList()
        {
            try
            {
                var res = GetTransferBL().GetAll();
                var list = res.Select(x => new { x.TransferCode, x.TransferName, x.DbHost, x.DbName }).ToList();
                return JsonSuccess(list);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 來源資料庫 Schema 查詢 ───────────────────────────────────────────────

        /// <summary>
        /// 依來源DB連線取得所有 TABLE 名稱
        /// </summary>
        [HttpGet("GetSourceTables")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create,FuncID.ESDbTransferMapping_Edit)]
        public IActionResult GetSourceTables([FromQuery] string dbTransferGuid)
        {
            try
            {
                var dm = GetTransferDm(dbTransferGuid);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                using var conn = CreateDbConnection(dm);
                conn.Open();
                return JsonSuccess(QueryTables(conn));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error") + ": " + ex.Message);
            }
        }

        /// <summary>
        /// 依來源DB連線 + TABLE 名稱取得欄位清單
        /// </summary>
        [HttpGet("GetSourceColumns")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create, FuncID.ESDbTransferMapping_Edit)]
        public IActionResult GetSourceColumns([FromQuery] string dbTransferGuid, [FromQuery] string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return JsonValidFail(GetMsg(_config, "Please_Enter") + " " + GetMsg(_config, "ESDbTransferMapping_SrcTable"));

                var dm = GetTransferDm(dbTransferGuid);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                using var conn = CreateDbConnection(dm);
                conn.Open();
                return JsonSuccess(QueryColumns(conn, tableName));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error") + ": " + ex.Message);
            }
        }

        // ── 目標資料庫 Schema 查詢 ───────────────────────────────────────────────

        /// <summary>
        /// 取得目的資料庫所有 TABLE 名稱（若指定 dbTransferGuid 則用該連線，否則用 MainConnection）
        /// </summary>
        [HttpGet("GetTargetTables")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create, FuncID.ESDbTransferMapping_Edit,FuncID.TableExcel_Create,FuncID.TableExcel_Edit)]
        public IActionResult GetTargetTables([FromQuery] string? dbTransferGuid = null)
        {
            try
            {
                List<string> tables;
                if (!string.IsNullOrEmpty(dbTransferGuid))
                {
                    var dm = GetTransferDm(dbTransferGuid);
                    if (dm == null)
                        return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                    using var conn = CreateDbConnection(dm);
                    conn.Open();
                    tables = QueryTables(conn);
                }
                else
                {
                    using var conn = new SqlConnection(_config.GetConnectionString("MainConnection"));
                    conn.Open();
                    tables = QueryTables(conn);
                }
                return JsonSuccess(tables);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error") + ": " + ex.Message);
            }
        }

        /// <summary>
        /// 取得目的資料庫指定 TABLE 的欄位清單（若指定 dbTransferGuid 則用該連線，否則用 MainConnection）
        /// </summary>
        [HttpGet("GetTargetColumns")]
        [CustomAuthorization(FuncID.ESDbTransferMapping_Create, FuncID.ESDbTransferMapping_Edit, FuncID.TableExcel_Create, FuncID.TableExcel_Edit)]
        public IActionResult GetTargetColumns([FromQuery] string tableName, [FromQuery] string? dbTransferGuid = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return JsonValidFail(GetMsg(_config, "Please_Enter") + " " + GetMsg(_config, "ESDbTransferMapping_DstTable"));

                List<string> columns;
                if (!string.IsNullOrEmpty(dbTransferGuid))
                {
                    var dm = GetTransferDm(dbTransferGuid);
                    if (dm == null)
                        return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                    using var conn = CreateDbConnection(dm);
                    conn.Open();
                    columns = QueryColumns(conn, tableName);
                }
                else
                {
                    using var conn = new SqlConnection(_config.GetConnectionString("MainConnection"));
                    conn.Open();
                    columns = QueryColumns(conn, tableName);
                }
                return JsonSuccess(columns);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error") + ": " + ex.Message);
            }
        }

        // ── 私有輔助方法 ─────────────────────────────────────────────────────────

        private ESDbTransferDM? GetTransferDm(string transferCode)
        {
            return GetTransferBL().GetAll().FirstOrDefault(x => x.TransferCode == transferCode);
        }

        private IDbConnection CreateDbConnection(ESDbTransferDM dm)
        {
            int dbType = int.TryParse(dm.DbType, out var t) ? t : 0;
            if (dbType == (int)EsDbTransferDbTypeEnum.PostgreSQL)
            {
                var port = string.IsNullOrWhiteSpace(dm.DbPort) ? "5432" : dm.DbPort;
                return new NpgsqlConnection($"Host={dm.DbHost};Port={port};Database={dm.DbName};Username={dm.DbUser};Password={dm.DbPassword}");
            }
            var portPart = string.IsNullOrWhiteSpace(dm.DbPort) ? "" : $",{dm.DbPort}";
            return new SqlConnection($"Data Source={dm.DbHost}{portPart};Initial Catalog={dm.DbName};User ID={dm.DbUser};Password={dm.DbPassword};TrustServerCertificate=true;Encrypt=true");
        }

        private static List<string> QueryTables(IDbConnection conn)
        {
            var tables = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                tables.Add(reader.GetString(0));
            return tables;
        }

        private static List<string> QueryColumns(IDbConnection conn, string tableName)
        {
            var columns = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tbl ORDER BY ORDINAL_POSITION";
            var p = cmd.CreateParameter();
            p.ParameterName = "@tbl";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(0));
            return columns;
        }
    }
}
