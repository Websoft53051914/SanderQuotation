using CommonClass.CustomAttribute;
using CommonClass.Model;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NPOI.SS.Formula.Functions;

namespace Core.Utility.Web.Base
{
    /// <summary>
    /// 基礎 Controller class
    /// </summary>
    public class ApiBaseController : Controller
    {
        /// <summary>
        /// Stores error messages encountered during processing.
        /// </summary>
        /// <remarks>This field can be used to collect and inspect error details after an operation
        /// completes. The list is initially empty and can be modified by consumers as needed.</remarks>
        public List<string> ErrorMsgs = new();
        /// <summary>功能說明：將錯誤訊息寫入 ViewBag，供 MVC 頁面顯示 Alert。</summary>
        /// <param name="msg">輸入參數：錯誤訊息文字。</param>
        /// <remarks>參考功能名稱與用途：ViewBag.ErrorAlertMessage。訊息內容及生成條件：由呼叫端傳入 msg，無 JSON 回應。</remarks>
        [ApiExplorerSettings(IgnoreApi = true)]
        public void ErrorAlert(String msg)
        {
            ViewBag.ErrorAlertMessage = msg;
        }

        /// <summary>功能說明：將警告訊息寫入 ViewBag，供 MVC 頁面顯示 Alert。</summary>
        /// <param name="msg">輸入參數：警告訊息文字。</param>
        /// <remarks>參考功能名稱與用途：ViewBag.WarningAlertMessage。訊息內容及生成條件：由呼叫端傳入，無 JSON 回應。</remarks>
        [ApiExplorerSettings(IgnoreApi = true)]
        public void WarningAlert(String msg)
        {
            ViewBag.WarningAlertMessage = msg;
        }



        /// <summary>功能說明：將 DataSourceRequest 轉為 BL 使用的 PageEntity（含排序、頁碼）。</summary>
        /// <param name="request">輸入參數：pageIndex、pageSize、SortField、SortOrder。</param>
        /// <returns>輸出參數：PageEntity（CurrentPage、PageDataSize、Sort、Asc）。</returns>
        /// <remarks>參考功能名稱與用途：供 Kendo/Grid 分頁查詢。訊息內容及生成條件：無 HTTP 回應。</remarks>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity(DataSourceRequest request)
        {

            return new PageEntity()
            {
                //Filter = GetFilterListWebRequest(base.Request),
                Sort = !string.IsNullOrEmpty(request.SortField) ? request.SortField : string.Empty,
                Asc = string.IsNullOrWhiteSpace(request.SortOrder) || (request.SortOrder.ToUpper() != "ASC" && request.SortOrder.ToUpper() != "DESC") ? "ASC" : request.SortOrder.ToUpper(),
                CurrentPage = request.pageIndex,
                PageDataSize = request.pageSize
            };
        }

        /// <summary>
        /// 取得分頁要傳入的值
        /// </summary>
        /// <param name="request">取得的page request內容</param>
        /// <returns>傳回PageEntity</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity<T>(DataSourceRequest request)
            where T : class
        {
            var result = new PageEntity()
            {
                CurrentPage = request.pageIndex,
                PageDataSize = request.pageSize,
                Asc = string.IsNullOrWhiteSpace(request.SortOrder) || (request.SortOrder.ToUpper() != "ASC" && request.SortOrder.ToUpper() != "DESC") ? "ASC" : request.SortOrder.ToUpper()
            };
            // 設定排序參數
            var propertyInfoList = typeof(T).GetProperties();
            var defaultSort = string.Empty;
            var defaultAsc = string.Empty;
            foreach (var item in propertyInfoList)
            {
                var attrSortColumn = (SortColumnAttribute)Attribute.GetCustomAttribute(item, typeof(SortColumnAttribute));
                if (attrSortColumn == null)
                {
                    continue;
                }

                var sort = attrSortColumn.ColumnName ?? item.Name;

                if (string.IsNullOrEmpty(request.SortField) && attrSortColumn.IsDefault)
                {
                    defaultSort = sort;
                    defaultAsc = attrSortColumn.DefaultSortOrder;
                }

                if (request.SortField == item.Name)
                {
                    result.Sort = sort;
                    break;
                }
            }

            if (string.IsNullOrEmpty(result.Sort))
            {
                result.Sort = defaultSort;
                result.Asc = defaultAsc;
            }

            return result;
        }

        /// <summary>
        /// 取得分頁要傳入的值（使用 ListPageEntity）
        /// </summary>
        /// <param name="request">ListPageEntity 分頁請求</param>
        /// <returns>傳回 PageEntity</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity(ListPageEntity request)
        {
            PageEntity result = new();
            result.Sort = !string.IsNullOrEmpty(request.SortField) ? request.SortField : string.Empty;
            result.Asc = string.IsNullOrWhiteSpace(request.SortDir) || (request.SortDir.ToUpper() != "ASC" && request.SortDir.ToUpper() != "DESC") ? "ASC" : request.SortDir.ToUpper();
            result.CurrentPage = request.Page;
            result.PageDataSize = request.PageSize;
            return result;
        }

        /// <summary>
        /// 取得分頁要傳入的值（使用 ListPageEntity，依 SortAttribute 設定排序）
        /// </summary>
        /// <typeparam name="T">含 SortAttribute 的 VM 型別</typeparam>
        /// <param name="request">ListPageEntity 分頁請求</param>
        /// <returns>傳回 PageEntity</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity<T>(ListPageEntity request)
            where T : class
        {
            PageEntity result = new();
            result.CurrentPage = request.Page;
            result.PageDataSize = request.PageSize;
            result.Asc = string.IsNullOrWhiteSpace(request.SortDir) || (request.SortDir.ToUpper() != "ASC" && request.SortDir.ToUpper() != "DESC") ? "ASC" : request.SortDir.ToUpper();

            var propertyInfoList = typeof(T).GetProperties();
            string defaultSort = string.Empty;
            string defaultAsc = string.Empty;

            foreach (var item in propertyInfoList)
            {
                SortAttribute? attrSort = (SortAttribute?)Attribute.GetCustomAttribute(item, typeof(SortAttribute));
                if (attrSort == null) continue;

                string sort = attrSort.ColumnName ?? item.Name;

                if (string.IsNullOrEmpty(request.SortField) && attrSort.IsDefault)
                {
                    defaultSort = sort;
                    defaultAsc = attrSort.DefaultSortOrder;
                }

                if (request.SortField == item.Name)
                {
                    result.Sort = sort;
                    break;
                }
            }

            if (string.IsNullOrEmpty(result.Sort))
            {
                result.Sort = defaultSort;
                result.Asc = defaultAsc;
            }

            return result;
        }


        //public static List<GridFilterVO> GetFilterListWebRequest(HttpRequest request)
        //{
        //    List<GridFilterVO> list = new List<GridFilterVO>();

        //    //預設1000個欄位，應該不會有grid超過
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        if (request.Params["filter[filters][" + i + "][field]"] == null)
        //        {
        //            break;
        //        }

        //        GridFilterVO vo = new GridFilterVO()
        //        {
        //            FieldId = request.Params["filter[filters][" + i + "][field]"],
        //            Operator = request.Params["filter[filters][" + i + "][operator]"],
        //            Value = request.Params["filter[filters][" + i + "][value]"]
        //        };

        //        list.Add(vo);
        //    }
        //    return list;
        //}


        /// <summary>
        /// 分頁使用的Json格式
        /// </summary>
        /// <param name="data">要回傳的資料</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonPage(DataSourceResult data)
        {
            return Json(data);
        }

        /// <summary>
        /// ajax回傳OK的Json
        /// </summary>
        /// <param name="data">要回傳的資料</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonOK<T>(T data)
        {
            return Json(data);
        }

        /// <summary>功能說明：AJAX 成功回應，標準格式 { Success: true, Data }。</summary>
        /// <typeparam name="T">輸出資料型別。</typeparam>
        /// <param name="data">輸入參數：要回傳給前端的承載物件。</param>
        /// <returns>輸出參數：JsonResult，Success=true，Data=data。</returns>
        /// <remarks>參考功能名稱與用途：多數 Controller 查詢成功時使用。訊息內容及生成條件：由呼叫端決定 data 內容。</remarks>
        protected JsonResult JsonSuccess<T>(T data)
        {
            return Json(new
            {
                Success = true,
                Data = data
            });
        }

        /// <summary>
        /// ajax回傳OK的Json
        /// </summary>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonOK()
        {
            return Json(new
            {
                Success = true
            });
        }



        /// <summary>
        /// ajax回傳OK的Json
        /// </summary>
        /// <param name="message">要回傳的訊息</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonOK(string message)
        {
            return Json(new
            {
                Success = true, 
                Data = message
            });
        }


        /// <summary>功能說明：AJAX 驗證/業務失敗回應，格式 { Success: false, Message }。</summary>
        /// <param name="errorData">輸入參數：錯誤訊息字串（如「資料不存在」、GetMsg System_Error）。</param>
        /// <returns>輸出參數：JsonResult，Success=false，Message=errorData。</returns>
        /// <remarks>參考功能名稱與用途：BL 驗證失敗、catch 區塊。訊息內容及生成條件：由呼叫端傳入 errorData。</remarks>
        protected JsonResult JsonValidFail(string errorData)
        {
            return Json(new
            {
                Success = false,
                Message = errorData
            });
        }
        /// <summary>
        /// 驗證錯誤使用
        /// </summary>
        /// <param name="errorData">錯誤訊息資料</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonValidFail<T1>(T1 errorData)
        {
            return Json(new
            {
                Success = false,
                Errors = errorData
            });
        }

        /// <summary>
        /// 驗證錯誤使用
        /// </summary>
        /// <typeparam name="T1">指定的類型</typeparam>
        /// <typeparam name="T2">指定的類型</typeparam>
        /// <param name="errorData">錯誤訊息資料</param>
        /// <param name="data">夾代要回傳的資料</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonValiFail<T1, T2>(T1 errorData, T2 data)
        {
            return Json(new
            {
                Success = false,
                Errors = errorData,
                Data = data
            });
        }

        /// <summary>
        /// 驗證錯誤使用
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <returns>回傳JsonResult</returns>
        protected JsonResult JsonValiFail(string message)
        {
            return Json(new
            {
                Success = false,
                Data = message
            });
        }

        /// <summary>功能說明：彙整 ModelState 驗證錯誤為單一 Message 字串並回傳失敗 JSON。</summary>
        /// <param name="modelstate">輸入參數：ASP.NET ModelState（含欄位驗證錯誤）。</param>
        /// <returns>輸出參數：JsonResult，Success=false，Message=合併後錯誤字串。</returns>
        /// <remarks>參考功能名稱與用途：表單 Model 驗證失敗。訊息內容及生成條件：任一欄位 Errors 非空時串接 ErrorMessage。</remarks>
        protected JsonResult JsonValiFailFromModelState(ModelStateDictionary modelstate)
        {
            string errorMsg = "";
            foreach (var item in modelstate.Where(w => w.Value.Errors.Count > 0).Select(w => w.Value.Errors.Select(s => s.ErrorMessage)))
            {
                errorMsg += string.Join(",", item) + ",";
            }

            return Json(new
            {
                Success = false,
                Message = errorMsg
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected string GetDomainName()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        }
    }
}
