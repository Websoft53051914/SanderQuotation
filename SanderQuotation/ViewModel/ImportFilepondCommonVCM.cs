using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class ImportFilepondCommonVCM : BaseVCM
    {
        /// <summary>
        /// filepond server url(必須為「/」結尾)
        /// </summary>
        public string? ServerUrl { get; set; }
    }
}
