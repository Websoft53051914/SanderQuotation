using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Const
{
    public class RegexConst
    {
        public const string LICENSENO_REGEX = @"^(?:[A-HJ-NP-Z]{3}-?\d{4}|\d{3}-?[A-HJ-NP-Z]{3}|\d{3}-?[A-HJ-NP-Z]{2})$";
    }
}
