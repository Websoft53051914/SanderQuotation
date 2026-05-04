using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Utility;
using backend.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Const.Enums;

namespace backend.Models
{
    public partial class SelectListHandler
    {
        private readonly IConfiguration _config;
        public SelectListHandler(IConfiguration configuration)
        {
            _config = configuration;
        }


        private AccountBL? accountBL = null;
        private AccountBL GetBLAccount()
        {
            accountBL ??= BLFactory.GetInstance<AccountBL>();
            return accountBL;
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


        public List<SelectListItem> GetSelectListIndustryType()
        {
            //迴圈001~033 050 060~062 099 0191

            var result = new List<SelectListItem>();
            for (int i = 1; i <= 33; i++)
            {
                result.Add(new SelectListItem(i.ToString("D3"), i.ToString("D3")));
            }
            result.Add(new SelectListItem("050", "050"));
            for (int i = 60; i <= 62; i++)
            {
                result.Add(new SelectListItem(i.ToString("D3"), i.ToString("D3")));
            }
            result.Add(new SelectListItem("099", "099"));
            result.Add(new SelectListItem("0191", "0191"));
            return result;
        }
    }

    public class SelectListItemCustom : SelectListItem
    {
        public string Data { get; set; }
    }
}
