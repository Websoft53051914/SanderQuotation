using backend.Common;
using Core.Utility.Helper.Excel;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.Json;

namespace backend.AI.Plugins
{
    /// <summary>
    /// Kernel Function Plugin：報表產生工具
    /// 負責：依據輸入資料產生 PDF 或 Excel 報表並回傳下載連結
    /// </summary>
    public class ReportPlugin
    {
        //private readonly string _storagePath = "/var/www/downloads"; // Ubuntu 存放目錄
        //private readonly string _baseUrl = "https://your-api.com/files/";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        private readonly PathProvider _pathProvider;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="environment"></param>
        /// <param name="pathProvider"></param>
        public ReportPlugin(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, PathProvider pathProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _pathProvider = pathProvider;
        }

        /// <summary>
        /// 根據傳入的標題與資料產生 Excel 檔案，儲存至 Uploads/AIExcel 目錄後回傳下載連結。
        /// </summary>
        [KernelFunction, Description("將標題與資料產生 Excel 報表供使用者下載，若使用者要求匯出資料或下載表格，請呼叫此功能，若使用者要求的功能做不到，請不要呼叫")]
        public Task<string> GenerateExcelReport(
            [Description("欄位標題的 JSON 陣列字串，例如：[\"料號\",\"描述\",\"單價\"]")] string headersJson,
            [Description("資料的 JSON 二維陣列字串，每個子陣列代表一列，例如：[[\"A001\",\"電容\",1.5],[\"A002\",\"電阻\",0.8]]")] string dataJson,
            [Description("檔案名稱（不含副檔名）")] string fileName)
        {
            try
            {
                string storagePath = _pathProvider.AIExcel;
                if (!Directory.Exists(storagePath))
                    Directory.CreateDirectory(storagePath);

                List<string> headers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(headersJson) ?? new();
                List<List<JsonElement>> dataRows = System.Text.Json.JsonSerializer.Deserialize<List<List<JsonElement>>>(dataJson) ?? new();

                ExcelWriterHelper excelHelper = new();
                excelHelper.CreateWorkBook(ExcelType.XSSF);
                excelHelper.CreateSheet("Sheet1");

                // 寫入標題列
                excelHelper.SetRowCellIndex(0, 0);
                foreach (string header in headers)
                    excelHelper.SetCellValue(header);

                // 寫入資料列
                for (int row = 0; row < dataRows.Count; row++)
                {
                    excelHelper.SetRowCellIndex(row + 1, 0);
                    foreach (JsonElement cell in dataRows[row])
                    {
                        if (cell.ValueKind == JsonValueKind.Number)
                            excelHelper.SetCellValue(cell.GetDouble());
                        else
                            excelHelper.SetCellValue(cell.ToString());
                    }
                }

                string fullName = $"{fileName}_{DateTime.Now:HHmmss}.xlsx";
                string filePath = Path.Combine(storagePath, fullName);
                excelHelper.SaveTo(filePath);

                string url = $"{_httpContextAccessor.HttpContext!.Request.Scheme}://{_httpContextAccessor.HttpContext!.Request.Host}/api/AIChat/DownloadAIFile?fileName={Uri.EscapeDataString(fullName)}";

                string downloadJson = JsonConvert.SerializeObject(new
                {
                    fileName = fullName,
                    url
                });

                return Task.FromResult(@$"請直接回覆(請勿修改) => [DOWNLOAD_BUTTON]{downloadJson}[/DOWNLOAD_BUTTON]");
            }
            catch (Exception ex)
            {
                AICommon.LogError(ex, string.Empty);
                return Task.FromResult($"產生 Excel 時發生錯誤(請嘗試修正，嘗試不得超過2次): {ex.Message}");
            }
        }
    }
}
