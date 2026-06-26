using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class SysSettingVM
    {
        public bool IsAIDecisionProcessDisplay { get; set; } = true;

        /// <summary>自動查價是否執行內部查價</summary>
        public bool IsRunInternalPricing { get; set; } = true;

        /// <summary>自動查價是否執行外部查價</summary>
        public bool IsRunExternalPricing { get; set; } = true;

        public List<ListItemVM> PreferredVendorList { get; set; } = [];

        public List<ListItemVM> BrandComparisonCategoryList { get; set; } = [];

        public class ListItemVM
        {
            public Guid Id { get; set; }
            public string Value { get; set; } = string.Empty;

            public string Type { get; set; } = string.Empty;
        }
    }
}
