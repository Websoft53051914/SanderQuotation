using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using static Const.Enums;

namespace backend.Controllers
{
    /// <summary>轉入檔案上傳 FilePond Controller</summary>
    [Route("api/FilepondEsFileTransferUpload")]
    public partial class FilepondEsFileTransferUploadController : BaseProjectController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _uploadDir = Path.Combine("Uploads", FileDirectoryConst.EsFileTransferUpload);

        /// <summary>建構子</summary>
        public FilepondEsFileTransferUploadController(
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment) : base(configuration)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        private EsFileTransferUploadBL? _blUpload = null;

        /// <summary>取得 BL 實例</summary>
        private EsFileTransferUploadBL GetBlEsFileTransferUpload()
        {
            _blUpload ??= GetBLInstance<EsFileTransferUploadBL>();
            return _blUpload;
        }

        // ── FilePond Process（一般上傳起始，回傳 tempGuid） ──────────────────────────

        /// <summary>FilePond Process — 接收分塊上傳起始，回傳暫存 GUID</summary>
        [CustomAuthorization(FuncID.Home_View)]
        [HttpPost("Process")]
        [DisableRequestSizeLimit]
        public IActionResult Process(IFormFile file)
        {
            string tempGuid = Guid.NewGuid().ToString();
            try
            {
                HttpContext.Request.EnableBuffering();
                return Ok(tempGuid);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest("系統錯誤");
            }
        }

        // ── FilePond Patch（分塊上傳） ──────────────────────────────────────────────

        /// <summary>FilePond Patch — 接收分塊資料並寫入暫存檔</summary>
        [CustomAuthorization(FuncID.Home_View)]
        [HttpPatch("Patch")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Patch(string id)
        {
            try
            {
                HttpContext.Request.EnableBuffering();
                string fileName = Request.Headers["Upload-Name"].ToString() ?? string.Empty;
                int length = Convert.ToInt32(Request.ContentLength);
                await ChunkUpload(Request.Body, id, fileName, length);
                return Ok();
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest("系統錯誤");
            }
        }

        // ── FilePond Revert（取消上傳） ─────────────────────────────────────────────

        /// <summary>FilePond Revert — 刪除暫存檔並邏輯刪除 DB 資料</summary>
        [CustomAuthorization(FuncID.Home_View)]
        [HttpDelete("Revert")]
        public async Task<IActionResult> Revert()
        {
            try
            {
                using StreamReader reader = new(Request.Body, Encoding.UTF8);
                string uploadId = await reader.ReadToEndAsync();

                SearchVO searchVO = new();
                searchVO.UploadIdEq = Guid.Parse(uploadId);

                GetBlEsFileTransferUpload().DeleteByFilter(searchVO);

                string dirPath = Path.Combine(_webHostEnvironment.ContentRootPath, _uploadDir);
                string[] files = Directory.GetFiles(dirPath, uploadId + ".*");
                foreach (string f in files)
                {
                    System.IO.File.Delete(f);
                }

                return Ok(uploadId);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest("系統錯誤");
            }
        }

        // ── ChunkUpload 輔助方法 ────────────────────────────────────────────────────

        /// <summary>
        /// 接收分塊資料並寫入暫存檔，最後一塊完成後建立暫存 DB 記錄
        /// </summary>
        private async Task ChunkUpload(Stream stream, string id, string fileName, int length)
        {
            string dirPath = Path.Combine(_webHostEnvironment.ContentRootPath, _uploadDir);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string pathFile = Path.Combine(dirPath, id + Path.GetExtension(fileName));

            if (System.IO.File.Exists(pathFile))
            {
                while (IsOccupied(pathFile))
                {
                    Thread.Sleep(500);
                }
            }

            using (FileStream fileStream = new(pathFile, FileMode.Append))
            {
                byte[] buffer = new byte[length];
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                        break;

                    fileStream.Write(buffer, 0, read);
                }
            }

            // 最後一塊（小於 chunk size 5MB）→ 建立暂存 DB 記錄
            if (length < 5000000)
            {
                EsFileTransferUploadDM dm = new();
                dm.FileName = fileName;
                dm.UploadId = Guid.Parse(id);

                GetBlEsFileTransferUpload().DoCreateUpload(dm);
            }
        }

        /// <summary>
        /// 判斷檔案是否被佔用中
        /// </summary>
        private static bool IsOccupied(string filePath)
        {
            FileStream? stream = null;
            try
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
        }
    }
}
