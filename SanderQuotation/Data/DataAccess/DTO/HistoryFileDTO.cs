using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.DTO
{
    public class HistoryFileDTO:HistoryFileEntity
    {
        public string UpdatedByName { set; get; } = string.Empty;
    }
}
