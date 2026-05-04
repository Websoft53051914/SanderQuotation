using AutoMapper;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.DB;
using Core.Utility.Utility;
using Business.Common;
using Business.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using NPOI.SS.Formula.Functions;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class MenuBL : BaseProjectBL
    {
        IUnitOfWork _unitOfWork;
        public MenuBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<SysFuncDM> GetFuncs(string memberAccount)
        {
            var dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            List<SysFuncDTO> dtos = dao.FindList(memberAccount);
            IMapper mapper = CommonUtility.CreateMapper<SysFuncDTO, SysFuncDM>();
            var dms = mapper.Map<List<SysFuncDM>>(dtos);
            return dms;
        }

        public SysFuncDM GetInfoByFuncId(string funcId)
        {
            var dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            SysFuncDTO dto = dao.GetInfoByFuncId(funcId);
            IMapper mapper = CommonUtility.CreateMapper<SysFuncDTO, SysFuncDM>();
            var dm = mapper.Map<SysFuncDM>(dto);
            return dm;
        }

        public Dictionary<string, List<MenuDM>> GetMnumList(string memberAccount)
        {
            var dao = _unitOfWork.Repository<ITB_SysFuncDAO>();

            List<SysFuncDTO> list = dao.FindList(memberAccount);

            //list = list.Where(w => w.Organization_Id == SessionVO.Organization_Id).ToList().GroupBy(g => g.SysFuncName).Select(s => s.FirstOrDefault()).ToList();
            list = list.ToList().GroupBy(g => g.SysFuncName).Select(s => s.FirstOrDefault()).ToList();

            Dictionary<string, List<MenuDM>> chirdrenMapList = new();

            //var siteDao = _unitOfWork.Repository<ITB_SiteDAO>();
            //var siteData = siteDao.FindByPk(SessionVO.Site_Id);
            //bool IsDayCare = false;
            //if (siteData != null && siteData.Site_Type == SiteTypeEnum.DayCare.ToInt().ToString())
            //{
            //    IsDayCare = true;
            //}

            foreach (var entity in list)
            {
                var result = chirdrenMapList;


                (string,int?) menuRes = MenuFuncNameDelegator(entity.Url, entity.SysFuncName ?? string.Empty);
                entity.SysFuncName = menuRes.Item1;
                if (result.ContainsKey(entity.ClassName) == false)
                {
                    result[entity.ClassName] = new List<MenuDM>();
                }
                //if (IsDayCare)
                //{
                //    if (entity.PermissionCode == FuncID.RequestMaintain_View.ToInt().ToString() || entity.PermissionCode == FuncID.RequestMaintain_Create.ToInt().ToString()
                //        || entity.PermissionCode == FuncID.RequestMaintain_Delete.ToInt().ToString() || entity.PermissionCode == FuncID.RequestMaintain_Edit.ToInt().ToString())
                //    {
                //        //
                //        result[entity.ClassName].Add(new MenuDM()
                //        {
                //            ClassName = entity.ClassName,
                //            PermissionCode = int.Parse(entity.PermissionCode),
                //            FuncName = entity.SysFuncName,
                //            Url = "/DayCare",
                //            Icon = entity.Icon,
                //            Type = entity.Type,
                //            FuncId = entity.Id,
                //        });
                //        continue;
                //    }
                string? mfgNumberPrintUrl = _Configuration["MfgNumberPrintUrl"];
                if (!string.IsNullOrEmpty(mfgNumberPrintUrl))
                {
                    Uri mfgNumberPrintUrlObj = new Uri(mfgNumberPrintUrl);
                    if (entity.Url == mfgNumberPrintUrlObj.AbsolutePath)
                    {
                        continue;
                    }
                }
                

                result[entity.ClassName].Add(new MenuDM()
                {
                    ClassName = entity.ClassName,
                    PermissionCode = int.Parse(entity.PermissionCode),
                    FuncName = entity.SysFuncName,
                    Url = entity.Url,
                    FuncId = entity.Id,
                    DataCount = menuRes.Item2
                });
            }

            return chirdrenMapList;
        }

        /// <summary>
        /// 依帳號取得功能權限，組成 Sidebar 樹狀 MENU 資料
        /// FuncClass → SysFunc (leaf)
        /// </summary>
        public CommonClass.Models.DispatcherResponse_Menu GetMainMenuTree(string memberAccount)
        {
            var dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var list = dao.FindListMenu(memberAccount);

            // 去重複：同 SysFuncName 只保留一筆
            list = list.GroupBy(g => g.SysFuncName).Select(s => s.First()).ToList();

            // 以 ClassName (FuncClass) 分組，建立第一層父節點
            var grouped = list
                .GroupBy(f => f.ClassName ?? string.Empty)
                .OrderBy(g => g.Min(f => f.Sequence));

            var parentMenus = new List<CommonClass.Models.DispatcherData_Menu>();

            foreach (var group in grouped)
            {
                string className = group.Key;
                // 安全地產生唯一 MenuCode（移除空白與特殊字元）
                string parentCode = "CLS_" + System.Text.RegularExpressions.Regex.Replace(className, @"[^\w]", "_");

                var children = group.Select(f => new CommonClass.Models.DispatcherData_Menu
                {
                    //SystemCode = "MES",
                    MenuCode   = "FUNC_" + f.Id.ToString("N"),
                    MenuName   = f.SysFuncName ?? f.Name,
                    ParentCode = parentCode,
                    MenuType   = "F",
                    Icon       = string.Empty,
                    IsVisible  = true,
                    IsExternal = false,
                    Url        = f.Url ?? "#",
                    SortNo     = f.Sequence ?? "999",
                    MenuLevel  = 2,
                    NodeType   = "L",
                    ModuleCode = parentCode,
                    Breadcrumb = $"{className} > {f.SysFuncName ?? f.Name}",
                    SubMenu    = new List<CommonClass.Models.DispatcherData_Menu>()
                }).ToList();

                parentMenus.Add(new CommonClass.Models.DispatcherData_Menu
                {
                    //SystemCode = "MES",
                    MenuCode   = parentCode,
                    MenuName   = className,
                    ParentCode = string.Empty,
                    MenuType   = "M",
                    Icon       = "ri-apps-2-line",
                    IsVisible  = true,
                    IsExternal = false,
                    Url        = "#",
                    SortNo     = group.Min(f => f.Sequence) ?? "999",
                    MenuLevel  = 1,
                    NodeType   = "P",
                    ModuleCode = parentCode,
                    Breadcrumb = className,
                    SubMenu    = children
                });
            }

            return new CommonClass.Models.DispatcherResponse_Menu
            {
                Data     = parentMenus,
                ErrorMsg = string.Empty
            };
        }

        private (string,int?) MenuFuncNameDelegator(string url, string funName)
        {
            //ITB_FixApplyDAO tB_FixApplyDAO = _unitOfWork.Repository<ITB_FixApplyDAO>();
            int dataCount = 0;
            try
            {
                switch (url)
                {
                    case "/FixApply":
                        //dataCount = tB_FixApplyDAO.GetCountByApplyStatus(FixApplyStatusEnum.UnProcess);
                        funName += $"<span class='txt-cnt'>({dataCount})</span>";

                        return (funName, dataCount);
                    default:
                        return (funName, null);
                }
                
            }
            catch (Exception)
            {
                return (funName, null);
            }
        }
    }
}
