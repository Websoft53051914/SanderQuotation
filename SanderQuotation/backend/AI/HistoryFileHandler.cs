using backend.AI.VO;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace backend.AI
{
    public class HistoryFileHandler
    {
        public IWebHostEnvironment? WebHostEnvironment { get; set; }
        private readonly Kernel _kernel;
        private readonly GeminiFileApiClient _fileService;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="kernel"></param>
        /// <param name="fileService"></param>
        public HistoryFileHandler(Kernel kernel, GeminiFileApiClient fileService)
        {
            _kernel = kernel;
            _fileService = fileService;
        }
        /// <summary>
        /// AI 相關操作
        /// </summary>
        /// <param name="dm"></param>
        /// <param name="messageHelper"></param>
        /// <returns></returns>
        public async Task HandleOfAI(HistoryFileDM dm, MessageHelper messageHelper)
        {


            // 摘要資料產生
            // 只要傳入的 FileSummary 為空就會觸發
            if (string.IsNullOrEmpty(dm.FileSummary))
            {
                try
                {
                    FileUploadInfo fileUploadInfo = BuildFileUploadInfo(dm);
                    dm.FileSummary = await GetFileSummaryAsync(fileUploadInfo);
                }
                catch (Exception)
                {
                    messageHelper.SetAlert($"【{dm.FileName}】無法產生摘要");
                    return;
                }
            }
           
        }

        /// <summary>
        /// 取得檔案路徑
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        public string GetFilePath(HistoryFileDM dm)
        {
            ArgumentNullException.ThrowIfNull(WebHostEnvironment);
            string result = Path.Combine(WebHostEnvironment.ContentRootPath, FileDirectoryConst.HistoryFile, dm.UploadId + Path.GetExtension(dm.FileName));
            return result;
        }

        /// <summary>
        /// 建立 FileUploadInfo 物件
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        public FileUploadInfo BuildFileUploadInfo(HistoryFileDM dm)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(dm.FileName);

            string filePath = GetFilePath(dm);

            FileUploadInfo result = new();
            result.fileStream = File.OpenRead(filePath);
            result.fileName = $"{dm.UploadId}_{dm.FileName ?? string.Empty}";
            result.fileMimeType = Common.Method.GetMimeType(result.fileName);
            result.fileSize = result.fileStream.Length;

            return result;
        }

        /// <summary>
        /// 產生檔案摘要，會將檔案上傳到 Gemini File API 並將回傳的 URI 插入對話中讓模型閱讀，最後產生摘要回傳
        /// </summary>
        /// <param name="fileInfo">要上傳的檔案資訊</param>
        /// <returns>檔案摘要內容</returns>
        public async Task<string> GetFileSummaryAsync(FileUploadInfo fileInfo)
        {
            var history = new ChatHistory(@"
你是一個專業的文件分析助理，擅長閱讀各類文件並整理重點摘要。
請使用繁體中文回答。
");

            try
            {
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0.1f,
                    TopK = 1,
                    TopP = 0.2f,
                    CandidateCount = 1
                };

                var chatService = _kernel.GetRequiredService<IChatCompletionService>();

                var msg = new ChatMessageContentItemCollection();

                #region 上傳檔案到 Gemini File API

                var uri = await _fileService.UploadFileAsync(fileInfo.fileStream, fileInfo.fileMimeType, fileInfo.fileName);

                msg.Add(new ImageContent(new Uri(uri))
                {
                    MimeType = fileInfo.fileMimeType
                });

                #endregion


                #region 摘要 Prompt

                msg.Add(new TextContent($@"
請閱讀附件檔案內容並產生摘要。

摘要目的是讓使用者快速了解文件內容，同時提供足夠語意資訊以利搜尋與向量比對。

摘要要求：

1. 使用繁體中文
2. 控制在 200 字內
3. 內容需包含以下資訊：

* 文件主題（清楚說明文件主要討論的主題或領域）
* 主要內容重點（條列2-4項）
* 關鍵資訊或數據（若有）
* 關鍵字（3-6個與主題高度相關的詞，例如技術名稱、領域名稱、應用情境）

如果是圖片，請描述：

* 圖片內容
* 可辨識文字
* 圖片用途或情境

格式要求：

* 只使用普通段落與 '-' 條列
* 不要使用 Markdown 標題符號 (#、##、###)
* 不要使用粗體或其他 Markdown 語法
* 段落之間請空一行
* 重點可以使用 '-' 條列

摘要內容請盡量包含具體名詞、技術名稱、主題領域或應用情境，以提升搜尋與語意比對的準確度。
"));

                #endregion

                history.AddUserMessage(msg);

                var response = await chatService.GetChatMessageContentAsync(
                    history,
                    executionSettings: settings,
                    kernel: _kernel
                );

                return response?.Content ?? "";
            }
            catch (Exception ex)
            {
                AICommon.LogError(ex, new List<string> { $"檔案摘要失敗: {fileInfo.fileName}" });
                throw;
            }
            finally
            {
                fileInfo.fileStream?.Dispose();
            }
        }
    }
}
