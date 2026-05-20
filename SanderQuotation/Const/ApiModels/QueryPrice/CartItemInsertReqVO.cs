using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const.ApiModels.QueryPrice
{
    public class CartItemInsertReqVO
    {   
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public class CartItem
        {
            public string MouserPartNumber { get; set; } = "";
            public int Quantity { get; set; }
            public string CustomerPartNumber { get; set; } = "";
        }
    }
}
