using AutoMapper;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.Excel;
using Data.DataAccess.DTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using ViewModel;

namespace backend.Controllers
{
    /// <summary>
    /// Sander 採購型號主檔 API Controller
    /// </summary>
    [Route("api/SanderModuleItem")]
    public partial class SanderModuleItemController : BaseProjectController
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        public SanderModuleItemController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : base(configuration)
        {
            _webHostEnvironment = webHostEnvironment;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<SanderModuleItemDM, SanderModuleItemVM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private SanderModuleItemBL? _blSanderModuleItem = null;

        /// <summary>
        /// 取得 SanderModuleItemBL 實例
        /// </summary>
        protected SanderModuleItemBL GetBlSanderModuleItem()
        {
            _blSanderModuleItem ??= GetBLInstance<SanderModuleItemBL>();

            return _blSanderModuleItem;
        }

        /// <summary>
        /// 分頁取得採購型號清單（從資料庫）
        /// </summary>
        [HttpGet("GetPageList")]
        public ActionResult GetPageList([FromQuery] SanderModuleItemSearchVM filter)
        {
            try
            {
                PageEntity pageEntity = GetPageEntity<SanderModuleItemVM>(filter);

                SearchVO searchVO = new();
                searchVO.KeywordLike = filter.KeywordLike;

                PageResult<SanderModuleItemDM> pageResult = GetBlSanderModuleItem().GetPageList(pageEntity, searchVO);

                List<SanderModuleItemVM> list = [];
                foreach (SanderModuleItemDM dm in pageResult.Results)
                {
                    SanderModuleItemVM vm = _mapper.Map<SanderModuleItemVM>(dm);

                    list.Add(vm);
                }

                return JsonSuccess(new
                {
                    Data = list,
                    Total = pageResult.DataCount,
                    Page = pageResult.CurrentPage,
                    PageSize = pageResult.PageDataSize,
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }

    public partial class SanderModuleItemController
    {
        /// <summary>
        /// 匯入內部料號資料
        /// </summary>
        /// <returns></returns>
        private void ImportSandermoduleItem()
        {
            try
            {
                string pathTmpl = Path.Combine(_webHostEnvironment.WebRootPath, "file", "SanderModule_Item.xlsx");
                ExcelReaderHelper reader = new();
                reader.SetWorkBook(pathTmpl);
                reader.SetSheet(reader.GetWorkBook().GetSheetAt(0));
                List<SanderModuleItemDM> dmList = [];
                int startRowIndex = 2;
                for (int i = startRowIndex; i <= reader.GetSheet().LastRowNum; i++)
                {
                    SanderModuleItemDM dm = new();
                    reader.SetRowCellIndex(i, 0);
                    dm.No = reader.GetStringValue()?.Trim() ?? string.Empty;
                    reader.NextCell();
                    dm.Description = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.Description2 = reader.GetStringValue()?.Trim();
                    reader.NextCell();
                    dm.LongDesc = reader.GetStringValue()?.Trim() ?? string.Empty;
                    reader.NextCell();
                    dm.LongDesc2 = reader.GetStringValue()?.Trim();

                    dmList.Add(dm);
                }
                reader.GetWorkBook().Dispose();

                foreach (SanderModuleItemDM dm in dmList)
                {
                    GetBlSanderModuleItem().DoCreate(dm);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
    }
}

