
using System.Diagnostics;

namespace frontend.Common
{
    /// <summary>
    /// libreoffice 轉檔
    /// </summary>
    public class LibreOfficeConverter
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;
        public LibreOfficeConverter(IWebHostEnvironment hostingEnvironment , IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }
        public  byte[] ExcelToOds(MemoryStream source)
        {
            return Todo(source, "xlsx", "ods");
        }

        public  byte[] WordToPdf(MemoryStream source)
        {
            return Todo(source, "docx", "pdf");
        }

        private byte[] Todo(MemoryStream source,string sourceFileExtension , string targetExtension)
        {
            bool IsLinuxServer = _configuration.GetValue<bool>("IsLinuxServer");

            string tempFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "TempFile");
            //如果資料夾不存在，則建立資料夾
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string rdName = $"{Guid.NewGuid().ToString()}.{sourceFileExtension}";
            string tempSourcePath = Path.Combine(_hostingEnvironment.ContentRootPath, "TempFile", rdName);

            using (FileStream fs = new FileStream(tempSourcePath, FileMode.Create))
            {
                source.WriteTo(fs);
            }

            string FileName = IsLinuxServer ? "libreoffice" : "C:\\Program Files\\LibreOffice\\program\\soffice.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = FileName,
                Arguments = $" --headless --convert-to {targetExtension} {tempSourcePath} --outdir {Path.GetDirectoryName(tempSourcePath)} ",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (IsLinuxServer)
            {
                startInfo.Environment["HOME"] = "/tmp";
            }

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

                process.WaitForExit();
            }
            string targetPath = tempSourcePath.Replace(sourceFileExtension, targetExtension);
            // 讀ods 寫入 FileStream 
            byte[] outPutFile = null;
            using (FileStream fs = new FileStream(targetPath, FileMode.Open))
            {
                outPutFile = new byte[fs.Length];
                fs.Read(outPutFile, 0, outPutFile.Length);
            }

            // 刪除Source
            try
            {
                System.IO.File.Delete(tempSourcePath);
            }
            catch
            {

            }

            // 刪除target
            try
            {
                System.IO.File.Delete(targetPath);
            }
            catch
            {

            }

            return outPutFile;
        }
    }
}
