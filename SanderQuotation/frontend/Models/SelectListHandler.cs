
using Core.Utility.Utility;
using frontend.Common;
using frontend.Common.ConfigurationHelper;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace frontend.Models
{
    public partial class SelectListHandler
    {
        private readonly ConfigurationHelper _configuration;
        public SelectListHandler(ConfigurationHelper configuration = null)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 取得 enum 類別項目清單
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<SelectListItem> GetSelectListEnum<T>()
            where T : struct, System.Enum
        {
            return ConvertUtility.Enum2Dictionary<T>()
                .Select(x => new SelectListItem(x.Value, x.Key.ToString()))
                .ToList();
        }

        public List<SelectListItem> GetI18NSelectListEnum<T>()
    where T : struct, System.Enum
        {
            return ConvertUtility.Enum2Dictionary<T>()
                .Select(x => new SelectListItem(_configuration.GetMessage(x.Value, x.Value), x.Key.ToString()))
                .ToList();
        }


    }

    public class SelectListItemCustom : SelectListItem
    {
        public SelectListItemCustom()
        {

        }

        public SelectListItemCustom(string text, string value) : base(text, value)
        {

        }

        public string? Data { get; set; }
        /// <summary>
        /// 上層值
        /// </summary>
        public string? ParentValue { get; set; }
        /// <summary>
        /// 自定義屬性
        /// </summary>
        public Dictionary<string, string> OtherAttr { get; set; } = new();
    }
}
