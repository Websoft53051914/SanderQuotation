using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class BaseVM
    {
        public Guid Id { set; get; }

        /// <summary>
        /// ª¬ºA
        /// </summary>
        public int? Status { set; get; }

        /// <summary>
        /// ¶µ¦¸
        /// </summary>
        public int No { get; set; }
    }
}
