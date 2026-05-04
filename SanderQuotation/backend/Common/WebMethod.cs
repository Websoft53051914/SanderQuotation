using backend.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace backend.Common
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

        internal static void SetUISetting(ViewDataDictionary viewData, BaseProjectController uIExampleController, string view)
        {
            var _UISetting = Method.GetUISetting();
            //var view = System.Reflection.MethodBase.GetCurrentMethod().Name.ToLower();
            var controller = uIExampleController.ControllerContext.RouteData.Values["controller"].ToString();
            var temp = _UISetting.Where(w => w.View.ToLower() == view.ToLower() && w.Controller.ToLower() == controller.ToLower()).FirstOrDefault();
            viewData["UISetting"] = temp;
        }

        public static IFormFile ConvertToFormFile(byte[] fileBytes, string fileName, string contentType = "application/octet-stream")
        {
            var stream = new MemoryStream(fileBytes);

            return new FormFile(stream, 0, fileBytes.Length, name: "file", fileName: fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
