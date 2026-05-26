namespace backend.Common
{
    /// <summary>
    /// 路徑中介站
    /// </summary>
    public class PathProvider
    {
        /// <summary>
        /// 根目錄路徑，通常是專案的 ContentRootPath
        /// </summary>
        public string Root { get; }
        /// <summary>
        /// 上傳目錄路徑
        /// </summary>
        public string Upload { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="env"></param>
        public PathProvider(IWebHostEnvironment env)
        {
            Root = env.ContentRootPath;
            Upload = Path.Combine(Root, "Uploads");
        }

        /// <summary>
        /// 轉入檔案上傳
        /// </summary>
        public string EsFileTransferUpload => Path.Combine(Upload, "EsFileTransferUpload");

        /// <summary>
        /// AI 產生的 Excel 暫存目錄
        /// </summary>
        public string AIExcel => Path.Combine(Upload, "AIExcel");
    }
}
