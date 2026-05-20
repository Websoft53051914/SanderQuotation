using AutoMapper;
using backend.AI;
using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.Message;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ViewModel.HistoryFile;
using static Const.Enums;

namespace backend.Controllers
{
    [Route("api/HistoryFile")]
    public class HistoryFileController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HistoryFileHandler _historyFileHandler;
        private readonly string _dir;

        public HistoryFileController(IConfiguration config, IWebHostEnvironment webHostEnvironment, HistoryFileHandler historyFileHandler) : base(config)
        {
            _config = config;
            _historyFileHandler = historyFileHandler;
            _historyFileHandler.WebHostEnvironment = webHostEnvironment;
            _webHostEnvironment = webHostEnvironment;
            _dir = FileDirectoryConst.HistoryFile;
        }

        private HistoryFileBL? historyFileBL;
        private HistoryFileBL GetHistoryFileBL()
        {
            historyFileBL ??= GetBLInstance<HistoryFileBL>();
            return historyFileBL;
        }


        [HttpPost("Process")]
        [CustomAuthorization(FuncID.HistoryFile_Create, FuncID.HistoryFile_Edit)]
        [DisableRequestSizeLimit]
        public IActionResult Process(IFormFile file)
        {
            var tempGuid = Guid.NewGuid().ToString();
            try
            {
                HttpContext.Request.EnableBuffering();

                return Ok(tempGuid);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest(GetMsg(_config, "System_Error"));
            }
        }

        [CustomAuthorization(FuncID.HistoryFile_Create, FuncID.HistoryFile_Edit)]
        [HttpPatch("Patch")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> Patch(string id)
        {
            try
            {
                HttpContext.Request.EnableBuffering();
                string Name = Request.Headers["Upload-Name"].ToString() ?? string.Empty;

                // 1. 取得當前碎片的長度
                int length = Convert.ToInt32(Request.ContentLength);

                // 3. 將數值傳入核心方法
                await ChunkUpload(Request.Body, id, Name, length);


                return Ok();
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest(GetMsg(_config, "System_Error"));
            }
        }

        static bool IsOccupied(string filePath)
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

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task ChunkUpload(Stream stream, string id, string fileName, int length)
        {
            string pathFile = Path.Combine(_webHostEnvironment.ContentRootPath, _dir, id + Path.GetExtension(fileName));

            if (!Directory.Exists(_dir))
            {
                Directory.CreateDirectory(_dir);
            }

            if (System.IO.File.Exists(pathFile))
            {
                while (IsOccupied(pathFile))
                {
                    Thread.Sleep(500); //延遲 0.5秒
                }
            }

            using (FileStream fileStream = new(pathFile, FileMode.Append))
            {
                var buffer = new byte[length];
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (read == 0)
                        break;

                    fileStream.Write(buffer, 0, read);
                }
            }

            if (length < 5000000)
            {
                HistoryFileDM dm = new();
                dm.FileName = fileName;
                dm.UploadId = id;

                GetHistoryFileBL().DoCreate(dm);
            }
        }

        /// <summary>
        /// 取消上傳
        /// </summary>
        /// <returns></returns>
        [CustomAuthorization(FuncID.HistoryFile_Create, FuncID.HistoryFile_Edit)]
        [HttpDelete("Revert")]
        public async Task<ActionResult> Revert()
        {
            try
            {
                using StreamReader reader = new(Request.Body, Encoding.UTF8);
                string uploadId = await reader.ReadToEndAsync();

                HistoryFileDM? dm = GetHistoryFileBL()
                    .GetByUploadId(uploadId);

                if (dm != null)
                {
                    GetHistoryFileBL().DoDelete(dm.Id);
                    if (!string.IsNullOrWhiteSpace(dm.UploadId) && !string.IsNullOrWhiteSpace(dm.FileName))
                    {
                        string pathFile = Path.Combine(_webHostEnvironment.ContentRootPath, _dir, dm.UploadId + Path.GetExtension(dm.FileName));
                        System.IO.File.Delete(pathFile);
                    }
                }

                return Ok(uploadId);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return BadRequest(GetMsg(_config, "System_Error"));
            }
        }




        /// <summary>
        /// 上傳
        /// </summary>
        /// <returns></returns>
        [CustomAuthorization(FuncID.HistoryFile_Create, FuncID.HistoryFile_Edit)]
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload([FromBody] HistoryFileEditVM vm)
        {
            try
            {
                DateTime now = DateTime.Now;

                MessageHelper messageHelper = new MessageHelper();
                if (vm.NewFileUploadIdList?.Count > 0)
                {
                    SearchVO searchVO = new();
                    searchVO.UploadIdIn = vm.NewFileUploadIdList;
                    searchVO.StatusEq = (int)StatusEnum.Disabled;
                    List<HistoryFileDM> dmList = GetHistoryFileBL().GetListByFilter(searchVO);
                    MessageHelper messageHelperSub = new MessageHelper();
                    foreach (HistoryFileDM dm in dmList)
                    {
                        messageHelperSub.Clear();

                        dm.UpdatedAt = now;
                        await _historyFileHandler.HandleOfAI(dm, messageHelperSub);

                        if (messageHelperSub.IsError())
                        {
                            messageHelper.SetAlert(messageHelperSub.GetAlert());
                        }
                    }

                    GetHistoryFileBL().DoBindBatch(dmList);
                }

                string msgSuccess = "執行成功";
                if (messageHelper.IsError())
                {
                    msgSuccess = msgSuccess + "\n" + messageHelper.GetAlert();
                }

                return JsonSuccess(msgSuccess);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }
    }
}
