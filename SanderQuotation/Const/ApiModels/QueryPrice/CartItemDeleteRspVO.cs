using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class CartItemDeleteRspVO
    {
        public string CartKey { get; set; } = "";

        public int TotalItemCount { get; set; }

        public List<MouserRspErrorVO> Errors { get; set; } = new List<MouserRspErrorVO>();
    }
}
