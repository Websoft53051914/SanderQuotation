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
