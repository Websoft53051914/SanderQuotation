using Core.Utility.Helper.DB.Entity;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
        /// <summary>
        /// Displays an error message alert.
        /// </summary>
        /// <param name="msg">The error message to display.</param>
        [ApiExplorerSettings(IgnoreApi = true)]
        public void ErrorAlert(String msg)
        {
            ViewBag.ErrorAlertMessage = msg;
        }

        /// <summary>
        /// 警告訊息alert
        /// </summary>
        /// <param name="msg">警告訊息</param>
        [ApiExplorerSettings(IgnoreApi = true)]
        public void WarningAlert(String msg)
        {
            ViewBag.WarningAlertMessage = msg;
        }



        /// <summary>
        /// 取得分頁要傳入的值
        /// </summary>
        /// <param name="request">取得的page request內容</param>
        /// <returns>傳回PageEntity</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected PageEntity GetPageEntity(DataSourceRequest request)
        {

            return new PageEntity()
            {
                //Filter = GetFilterListWebRequest(base.Request),
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

        /// <summary>
        /// ajax回傳OK的Json
        /// </summary>
        /// <typeparam name="T">取得的page request內容</typeparam>
        /// <param name="data">要回傳的資料</param>
        /// <returns>回傳JsonResult</returns>
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


        /// <summary>
        /// 驗證錯誤使用
        /// </summary>
        /// <param name="errorData">錯誤訊息資料</param>
        /// <returns>回傳JsonResult</returns>
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

        // TODO
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelstate"></param>
        /// <returns>回傳JsonResult</returns>
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
