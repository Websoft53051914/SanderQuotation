using Microsoft.AspNetCore.Mvc.ViewFeatures;
using frontend.Controllers;

namespace frontend.Common
{
    public partial class WebMethod
    {
        internal static string GetClassName(HttpRequest request)
        {
            return request.Query["className"];
        }

        internal static string SetFuncIdAndClassName(ViewDataDictionary viewData, HttpRequest request)
        {
            viewData["className"] = request.Query["className"];
            viewData["FuncId"] = request.Query["funcid"];
            string funcId = request.Query["funcid"];
            return funcId;
        }
        internal static string SetFuncIdAndClassName(ViewDataDictionary viewData, string className, string funcid)
        {
            viewData["className"] = className;
            viewData["FuncId"] = funcid;
            string funcId = funcid;
            return funcId;
        }
    }
}
