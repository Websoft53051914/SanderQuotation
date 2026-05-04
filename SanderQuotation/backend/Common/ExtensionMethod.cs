using CommonClass.Model;
using Core.Utility.Web.HtmlHelperCustom;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Dynamic;
using System.Globalization;

namespace backend.Common
{
    public static class ExtensionMethod
    {
        /// <summary>
        ///  ex: 2024/12/2 下午 04:43:42
        /// </summary>
        /// <param name="dtTime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToStringCulture_zh_TW(this DateTime dtTime, string format = "")
        {
            return dtTime.ToString(format, new System.Globalization.CultureInfo("zh-TW"));
        }

        /// <summary>
        ///  轉民國日期字串
        /// </summary>
        /// <param name="dtTime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToTaiwanCalendarString(this DateTime dtTime, string format = "yyy/MM/dd")
        {
            TaiwanCalendar taiwanCalendar = new TaiwanCalendar();
            CultureInfo taiwanCulture = new CultureInfo("zh-TW");
            taiwanCulture.DateTimeFormat.Calendar = taiwanCalendar;

            return dtTime.ToString(format, taiwanCulture);
        }

        

        public static string ToChineseDayOfWeek(this DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "一",
                DayOfWeek.Tuesday => "二",
                DayOfWeek.Wednesday => "三",
                DayOfWeek.Thursday => "四",
                DayOfWeek.Friday => "五",
                DayOfWeek.Saturday => "六",
                DayOfWeek.Sunday => "日",
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        public static int CalculateAge(this DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
                age--;

            return age;
        }

        public static async Task<byte[]> GetBytes(this IFormFile formFile)
        {
            await using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private static string RouteData(this IHtmlHelper htmlHelper, string key)
        {
            return htmlHelper.ViewContext.RouteData.Values[key].ToString();
        }

        public static string ControllerName(this IHtmlHelper htmlHelper)
        {
            return htmlHelper.RouteData("controller");
        }

        public static string GetActionName(this IHtmlHelper htmlHelper)
        {
            var temp = new { };

            var temp2 = new { temp = temp };


            return htmlHelper.RouteData("action");
        }

        public static string AreaName(this IHtmlHelper htmlHelper)
        {
            return htmlHelper.ViewContext.RouteData.DataTokens["area"] as string;
        }


        public static UIElements ByElementId(this UISetting _UISetting, string elementId)
        {
            if (_UISetting.Elements != null)
            {
                var result = _UISetting.Elements.Where(w => w.ElementId == elementId);
                if (result == null || result.Count() == 0)
                {
                    return new UIElements() { Maxlength = 100 };
                }

                return result.FirstOrDefault();
            }

            return new UIElements();
        }

        public static string GetAllAttrByElementId(this UISetting _UISetting, string elementId)
        {
            if (_UISetting.Elements != null)
            {
                var result = _UISetting.Elements.Where(w => w.ElementId == elementId);
            }

            return "";
        }

        public static IDictionary<string, object> GetAttr(this UISetting _UISetting, string elementId, object htmlAttributes = null)
        {
            if (htmlAttributes == null)
                htmlAttributes = new { };

            var allAttrdic = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (_UISetting != null && _UISetting.Elements != null && _UISetting.Elements.Count > 0)
            {
                var tempdic = _UISetting.GetAllAttrForIDic(elementId);
                foreach (var item in tempdic)
                {
                    if (!allAttrdic.ContainsKey(item.Key))
                        allAttrdic.Add(item.Key, item.Value);
                    else
                    {
                        allAttrdic[item.Key] = item.Value;
                    }
                }
            }

            return allAttrdic;
        }

        public static string GetAttrStr(this UISetting _UISetting, string elementId)
        {
            var allAttrdic = HtmlHelper.AnonymousObjectToHtmlAttributes(new { });
            if (_UISetting != null && _UISetting.Elements != null && _UISetting.Elements.Count > 0)
            {
                var tempdic = _UISetting.GetAllAttrForIDic(elementId);
                foreach (var item in tempdic)
                {
                    if (!allAttrdic.ContainsKey(item.Key))
                        allAttrdic.Add(item.Key, item.Value);
                    else
                    {
                        allAttrdic[item.Key] = item.Value;
                    }
                }
            }

            return string.Join(" ", allAttrdic.Select(s => $"{s.Key}={s.Value}"));
        }


        public static string GetRequiredForLabel(this UISetting _UISetting, string elementId)
        {
            if (_UISetting.Elements != null)
            {
                var element = _UISetting.Elements.Where(w => w.ElementId == elementId);
                if (element != null && element.Count() > 0 && element.FirstOrDefault().IsRequired == true)
                    return "required";
            }
            return string.Empty;
        }

        public static HtmlString GetAllAttr(this UISetting _UISetting, string elementId)
        {
            List<string> attrList = new List<string>();

            if (_UISetting.Elements != null)
            {
                var element = _UISetting.Elements.Where(w => w.ElementId == elementId);
                if (element != null && element.Count() > 0 && element.FirstOrDefault().IsRequired == true)
                {
                    var item = element.FirstOrDefault();
                    if (item.IsRequired == true)
                        attrList.Add("required");

                    if (!string.IsNullOrEmpty(item.Type))
                        attrList.Add($"type={item.Type}");

                    if (!string.IsNullOrEmpty(item.Placeholder))
                        attrList.Add($"Placeholder={item.Placeholder}");

                    if (item.Maxlength != 0)
                        attrList.Add($"Maxlength={item.Maxlength}");

                    if (!string.IsNullOrEmpty(item.Pattern))
                        attrList.Add($"Pattern={item.Pattern}");

                    if (!string.IsNullOrEmpty(item.Name))
                        attrList.Add($"Name={item.Name}");

                    return string.Join(" ", attrList).Replace("'", @"").ToHtmlString();
                }
            }

            return HtmlString.Empty;
        }


        public static IDictionary<string, object> GetAllAttrForIDic(this UISetting _UISetting, string elementId)
        {
            IDictionary<string, object> dic = new Dictionary<string, object>();

            if (_UISetting.Elements != null)
            {
                var element = _UISetting.Elements.Where(w => w.ElementId == elementId);
                if (element != null && element.Count() > 0 && element.FirstOrDefault().IsRequired == true)
                {
                    var item = element.FirstOrDefault();
                    if (item.IsRequired == true)
                        dic.Add("required", "");

                    if (!string.IsNullOrEmpty(item.Type))
                        dic.Add($"type", item.Type);

                    if (!string.IsNullOrEmpty(item.Placeholder))
                        dic.Add($"Placeholder", item.Placeholder);

                    if (item.Maxlength != 0)
                        dic.Add($"Maxlength", item.Maxlength);

                    if (!string.IsNullOrEmpty(item.Pattern))
                        dic.Add($"Pattern", item.Pattern);

                    if (!string.IsNullOrEmpty(item.Name))
                        dic.Add($"Name", item.Name);
                }
            }
            return dic;
        }
    }
}
