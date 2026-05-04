using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const
{
    public static class Extension
    {

        public static string ToValueString(this Enum enumValue)
        {
            return Convert.ToInt32(enumValue).ToString();
        }
    }
}
