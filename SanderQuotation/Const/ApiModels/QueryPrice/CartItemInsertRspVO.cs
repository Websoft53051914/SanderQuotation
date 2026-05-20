using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class CartItemInsertRspVO
    {
        public string CartKey { get; set; } = "";

        public List<MouserRspErrorVO> Errors { get; set; } = new List<MouserRspErrorVO>();

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public class CartItem
        {
            public string MouserPartNumber { get; set; } = "";
            public decimal UnitPrice { get; set; }
            public decimal Price { get; set; }
            public decimal ExtendedPrice { get; set; }
            public List<MouserRspErrorVO> Errors { get; set; } = new List<MouserRspErrorVO>();
        }
    }
}
