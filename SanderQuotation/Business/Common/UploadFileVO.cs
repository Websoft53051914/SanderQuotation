using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Common
{
    public class UploadFileVO
    {
        public string FileName { get; set; } = "";

        public string FileFormat { get; set; } = "";
        public string ContentType { get; set; } = "";

        public long FileSize { get; set; } = 0;
        public byte[] FileContent { get; set; } = [];
    }
}
