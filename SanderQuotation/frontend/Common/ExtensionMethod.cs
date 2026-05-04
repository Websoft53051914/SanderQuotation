using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace frontend.Common.ExtensionMethod
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

        //public static HtmlString? ToHtmlString(this string? str)
        //{
        //    if (string.IsNullOrEmpty(str))
        //        return null;

        //    HtmlString htmlStr = new HtmlString(str);
        //    return htmlStr;
        //}

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


    }
}
