using AutoMapper;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.SqlClient;
using ViewModel;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/ESDbTransfer")]
    public class ESDbTransferController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public ESDbTransferController(IConfiguration config) : base(config)
        {
            _config = config;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ESDbTransferVM, ESDbTransferDM>().ReverseMap();
            });
            _mapper = mapperConfig.CreateMapper();
        }

        private ESDbTransferBL? _ESDbTransferBL;
        private ESDbTransferBL GetESDbTransferBL()
        {
            _ESDbTransferBL ??= GetBLInstance<ESDbTransferBL>();
            return _ESDbTransferBL;
        }

        [HttpGet("GetPageList")]
        [CustomAuthorization(FuncID.ESDbTransfer_View)]
        public IActionResult GetPageList([FromQuery] DataSourceRequest request, string Keyword)
        {
            try
            {
                PageEntity pageEntity = base.GetPageEntity(request);
                IMapper mapper = CommonUtility.CreateMapper<ESDbTransferVM, ESDbTransferDM>();
    
                SearchVO searchVO = new SearchVO
                {
                    KeywordLike = Keyword
                };
                var pageResult = GetESDbTransferBL().GetPageList(pageEntity, searchVO);

                var list = _mapper.Map<List<ESDbTransferVM>>(pageResult.Results);

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.No = (request.pageIndex - 1) * request.pageSize + i + 1;
                    item.DbTypeDescription = EnumUtility.GetDescriptionByInt<EsDbTransferDbTypeEnum>(int.Parse(item.DbType));
                    if (string.IsNullOrEmpty(list[i].Description))
                        list[i].Description = "";

                    item.CanDelete = pageResult.Results[i].DbTransferMappingDMs.Count == 0;
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
        [CustomAuthorization(FuncID.ESDbTransfer_Edit)]
        public IActionResult Get(Guid id)
        {
            try
            {
                var dm = GetESDbTransferBL().Get(id);
                if (dm == null)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                var vm = _mapper.Map<ESDbTransferVM>(dm);
                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        [HttpPost("Create")]
        [CustomAuthorization(FuncID.ESDbTransfer_Create)]
        public IActionResult Create(ESDbTransferVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferDM>(vm);
                GetESDbTransferBL().CheckExist(dm);
                if (GetESDbTransferBL().GetMessage().IsError())
                {
                    return JsonValidFail(GetESDbTransferBL().GetMessage().GetErrMsg());
                }
                var guid = GetESDbTransferBL().Create(dm);
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
        [CustomAuthorization(FuncID.ESDbTransfer_Edit)]
        public IActionResult Edit(ESDbTransferVM vm)
        {
            try
            {
                var dm = _mapper.Map<ESDbTransferDM>(vm);
                GetESDbTransferBL().Edit(dm);
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

        [HttpPost("TestConnection")]
        [CustomAuthorization(FuncID.ESDbTransfer_View)]
        public IActionResult TestConnection(ESDbTransferVM vm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vm.DbType) ||
                    string.IsNullOrWhiteSpace(vm.DbHost) ||
                    string.IsNullOrWhiteSpace(vm.DbName) ||
                    string.IsNullOrWhiteSpace(vm.DbUser))
                    return JsonValidFail("請填寫完整連線資訊");

                int dbType = int.Parse(vm.DbType);
                var portPart = string.IsNullOrWhiteSpace(vm.DbPort) ? "" : $",{vm.DbPort}";

                // EsDbTransferDbTypeEnum: PostgreSQL = 1, MSSQL = 2
                if (dbType == (int)EsDbTransferDbTypeEnum.MSSQL)
                {
                    var connStr = $"Data Source={vm.DbHost}{portPart};Initial Catalog={vm.DbName};User ID={vm.DbUser};Password={vm.DbPassword};TrustServerCertificate=true;Encrypt=true;Connect Timeout=5";
                    using var conn = new SqlConnection(connStr);
                    conn.Open();
                    conn.Close();
                }
                else if (dbType == (int)EsDbTransferDbTypeEnum.PostgreSQL)
                {
                    var port = string.IsNullOrWhiteSpace(vm.DbPort) ? "5432" : vm.DbPort;
                    var connStr = $"Host={vm.DbHost};Port={port};Database={vm.DbName};Username={vm.DbUser};Password={vm.DbPassword};Timeout=5;Command Timeout=5";
                    using var conn = new NpgsqlConnection(connStr);
                    conn.Open();
                    conn.Close();
                }
                else
                {
                    return JsonValidFail("不支援的資料庫類型");
                }

                return JsonSuccess("連線成功");
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return JsonValidFail("連線失敗");
            }
        }

        [HttpPost("Delete")]
        [CustomAuthorization(FuncID.ESDbTransfer_Delete)]
        public IActionResult Delete(List<Guid> list)
        {
            try
            {
                if (list == null || list.Count == 0)
                    return JsonValidFail(GetMsg(_config, "Data_Not_Found"));

                GetESDbTransferBL().Delete(list);
                foreach (var item in list)
                {
                    LogSuccess(item, LogAction.Delete);
                }
                return JsonSuccess("刪除成功");

            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
