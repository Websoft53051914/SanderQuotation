using System.Text.Json;

namespace backend.Models
{
    /// <summary>
    /// 轉入檔案上傳 — 主檔資料模型
    /// </summary>
    public class ImportTransferExcelData
    {
        /// <summary>主鍵</summary>
        public long Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        public string BomFileName { get; set; } = "";

        /// <summary>匯入設定規則名稱</summary>
        public string ImportRuleName { get; set; } = "";

        /// <summary>客戶名稱</summary>
        public string CustomerName { get; set; } = "";

        /// <summary>產品料號</summary>
        public string ProductNo { get; set; } = "";

        /// <summary>客戶別</summary>
        public string CustomerType { get; set; } = "";

        /// <summary>採購數量</summary>
        public int PurchaseQty { get; set; }

        /// <summary>狀態代碼（0=未轉檔、1=已轉檔、2=未查料、3=已查料、4=已查價）</summary>
        public int StatusCode { get; set; }

        /// <summary>建立日期</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>最後更新日期</summary>
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 轉入檔案上傳 — JSON 資料存取輔助類別
    /// </summary>
    public static class ImportTransferExcelJsonHelper
    {
        private static readonly string _filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "importtransferexcel.json");

        private static readonly JsonSerializerOptions _opt = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>轉換狀態代碼為顯示文字。</summary>
        public static string StatusText(int code) => code switch
        {
            0 => "未轉檔",
            1 => "已轉檔",
            2 => "未查料",
            3 => "已查料",
            4 => "已查價",
            _ => "未知"
        };

        /// <summary>讀取所有資料。</summary>
        private static List<ImportTransferExcelData> ReadAll()
        {
            if (!File.Exists(_filePath)) return new List<ImportTransferExcelData>();
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ImportTransferExcelData>>(json, _opt)
                   ?? new List<ImportTransferExcelData>();
        }

        /// <summary>寫回所有資料。</summary>
        private static void WriteAll(List<ImportTransferExcelData> list)
        {
            string json = JsonSerializer.Serialize(list, _opt);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>取得清單，支援關鍵字搜尋（BOM 檔案名稱、客戶名稱、產品料號、匯入設定規則）。</summary>
        public static List<ImportTransferExcelData> GetList(string? keyword = null)
        {
            List<ImportTransferExcelData> list = ReadAll();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                list = list.Where(r =>
                    r.BomFileName.ToLower().Contains(kw) ||
                    r.CustomerName.ToLower().Contains(kw) ||
                    r.ProductNo.ToLower().Contains(kw) ||
                    r.ImportRuleName.ToLower().Contains(kw)
                ).ToList();
            }
            return list;
        }

        /// <summary>依 Id 取得單筆資料。</summary>
        public static ImportTransferExcelData? GetById(long id)
        {
            return ReadAll().FirstOrDefault(r => r.Id == id);
        }

        /// <summary>更新客戶名稱與產品料號（編輯功能）。</summary>
        public static void SaveEdit(long id, string customerName, string productNo)
        {
            List<ImportTransferExcelData> list = ReadAll();
            ImportTransferExcelData? item = list.FirstOrDefault(r => r.Id == id);
            if (item == null) return;
            item.CustomerName = customerName;
            item.ProductNo = productNo;
            item.UpdateTime = DateTime.Now;
            WriteAll(list);
        }

        /// <summary>依 Id 刪除一筆資料。</summary>
        public static bool Delete(long id)
        {
            List<ImportTransferExcelData> list = ReadAll();
            int removed = list.RemoveAll(r => r.Id == id);
            if (removed > 0) WriteAll(list);
            return removed > 0;
        }
    }
}
