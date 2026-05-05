using System.Text.Json;

namespace backend.Models
{
    /// <summary>
    /// 定時查價結果 — 查價檔案主檔資料模型
    /// </summary>
    public class QuotationFileData
    {
        public long Id { get; set; }

        /// <summary>BOM 檔案名稱</summary>
        public string BomFileName { get; set; } = "";

        /// <summary>客戶名稱</summary>
        public string CustomerName { get; set; } = "";

        /// <summary>產品料號</summary>
        public string ProductNo { get; set; } = "";

        /// <summary>客戶別（MM / D3 / STL / CTB / WT / AED / EDS）</summary>
        public string CustomerType { get; set; } = "";

        /// <summary>採購數量</summary>
        public int PurchaseQty { get; set; }

        public long Creator { get; set; }
        public DateTime CreateTime { get; set; }
        public long Updater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int Status { get; set; }
    }

    /// <summary>
    /// 定時查價結果 — BOM 料項資料模型（含內部採購紀錄與外部查價）
    /// </summary>
    public class QuotationItemData
    {
        public long Id { get; set; }

        /// <summary>所屬查價檔案 ID</summary>
        public long FileId { get; set; }

        // ── BOM 基本資料 ──────────────────────────────────────────────
        public string Description { get; set; } = "";
        public string Manufacturer1 { get; set; } = "";
        public string ManufacturerPartNo1 { get; set; } = "";
        public int Quantity { get; set; }

        // ── 內部採購紀錄 ──────────────────────────────────────────────
        public string InternalProcurementDate { get; set; } = "";
        public decimal? InternalUnitPriceOrig { get; set; }
        public decimal? InternalUnitPriceTwd { get; set; }
        public int? InternalQty { get; set; }
        public string InternalCurrency { get; set; } = "";
        public string InternalSupplierName { get; set; } = "";

        // ── 外部查價 ──────────────────────────────────────────────────
        public string ExternalQuotationDate { get; set; } = "";
        public decimal? ExternalUnitPriceOrig { get; set; }
        public decimal? ExternalUnitPriceTwd { get; set; }
        public int? ExternalMOQ { get; set; }
        public string ExternalCurrency { get; set; } = "";
        public string ExternalSupplierName { get; set; } = "";

        // ── 可修改欄位 ────────────────────────────────────────────────
        /// <summary>採購型號（可修改）</summary>
        public string ProcurementModel { get; set; } = "";

        /// <summary>是否為建議採購型號</summary>
        public bool IsRecommended { get; set; }

        /// <summary>可選採購型號清單（select2 選項來源）</summary>
        public List<string> ProcurementModelOptions { get; set; } = new();
    }

    // ── 內部 JSON 根節點 ──────────────────────────────────────────────
    internal class QuotationDbRoot
    {
        public List<QuotationFileData> QuotationFiles { get; set; } = new();
        public List<QuotationItemData> QuotationItems { get; set; } = new();
    }

    /// <summary>
    /// 以 quotation.json 模擬資料庫的 CRUD Helper（定時查價結果）
    /// </summary>
    public class QuotationJsonHelper
    {
        private static readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static QuotationJsonHelper()
        {
            string baseDir = AppContext.BaseDirectory;
            _filePath = Path.Combine(baseDir, "quotation.json");

            if (!File.Exists(_filePath))
            {
                DirectoryInfo? dir = new DirectoryInfo(baseDir);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "quotation.json");
                    if (File.Exists(candidate))
                    {
                        _filePath = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
            }
        }

        #region -- 私有 IO --

        /// <summary>從 JSON 檔讀取資料庫快照。</summary>
        private static QuotationDbRoot Load()
        {
            if (!File.Exists(_filePath))
                return new QuotationDbRoot();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<QuotationDbRoot>(json, _jsonOpts) ?? new QuotationDbRoot();
        }

        /// <summary>將資料庫快照寫入 JSON 檔。</summary>
        private static void Save(QuotationDbRoot db)
        {
            string json = JsonSerializer.Serialize(db, _jsonOpts);
            File.WriteAllText(_filePath, json);
        }

        #endregion

        #region -- 查價檔案 CRUD --

        /// <summary>取得查價檔案清單（支援關鍵字搜尋）</summary>
        public static List<QuotationFileData> GetFileList(string? keyword = null)
        {
            QuotationDbRoot db = Load();
            List<QuotationFileData> list = db.QuotationFiles.Where(f => f.Status != 9).ToList();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                list = list.Where(f =>
                    f.BomFileName.ToLower().Contains(kw) ||
                    f.CustomerName.ToLower().Contains(kw) ||
                    f.ProductNo.ToLower().Contains(kw) ||
                    f.CustomerType.ToLower().Contains(kw)
                ).ToList();
            }
            return list.OrderByDescending(f => f.UpdateTime).ToList();
        }

        /// <summary>依 Id 取得單筆查價檔案。</summary>
        public static QuotationFileData? GetFileById(long id)
        {
            QuotationDbRoot db = Load();
            return db.QuotationFiles.FirstOrDefault(f => f.Id == id);
        }

        /// <summary>取得指定檔案的 BOM 料項清單。</summary>
        public static List<QuotationItemData> GetItemsByFileId(long fileId)
        {
            QuotationDbRoot db = Load();
            return db.QuotationItems.Where(i => i.FileId == fileId).OrderBy(i => i.Id).ToList();
        }

        /// <summary>取得指定檔案的料項數量。</summary>
        public static int GetItemCount(long fileId)
        {
            QuotationDbRoot db = Load();
            return db.QuotationItems.Count(i => i.FileId == fileId);
        }

        /// <summary>儲存（新增或更新）查價檔案 Header。</summary>
        public static QuotationFileData SaveFile(QuotationFileData file)
        {
            QuotationDbRoot db = Load();

            if (file.Id == 0)
            {
                file.Id = db.QuotationFiles.Any() ? db.QuotationFiles.Max(f => f.Id) + 1 : 1;
                file.CreateTime = DateTime.Now;
                file.UpdateTime = DateTime.Now;
                db.QuotationFiles.Add(file);
            }
            else
            {
                QuotationFileData? existing = db.QuotationFiles.FirstOrDefault(f => f.Id == file.Id);
                if (existing != null)
                {
                    existing.CustomerType = file.CustomerType;
                    existing.PurchaseQty = file.PurchaseQty;
                    existing.Updater = file.Updater;
                    existing.UpdateTime = DateTime.Now;
                }
            }

            Save(db);
            return file;
        }

        /// <summary>批次更新 BOM 料項的採購型號。</summary>
        public static void SaveItemProcurementModels(List<QuotationItemData> updates)
        {
            QuotationDbRoot db = Load();

            foreach (QuotationItemData update in updates)
            {
                QuotationItemData? existing = db.QuotationItems.FirstOrDefault(i => i.Id == update.Id);
                if (existing != null)
                    existing.ProcurementModel = update.ProcurementModel;
            }

            Save(db);
        }

        /// <summary>軟刪除查價檔案（Status = 9）。</summary>
        public static bool DeleteFile(long id)
        {
            QuotationDbRoot db = Load();
            QuotationFileData? file = db.QuotationFiles.FirstOrDefault(f => f.Id == id);
            if (file == null) return false;

            file.Status = 9;
            file.UpdateTime = DateTime.Now;
            Save(db);
            return true;
        }

        /// <summary>批次軟刪除查價檔案。</summary>
        public static int BatchDeleteFiles(List<long> ids)
        {
            QuotationDbRoot db = Load();
            int count = 0;

            foreach (long id in ids)
            {
                QuotationFileData? file = db.QuotationFiles.FirstOrDefault(f => f.Id == id);
                if (file == null) continue;

                file.Status = 9;
                file.UpdateTime = DateTime.Now;
                count++;
            }

            Save(db);
            return count;
        }

        /// <summary>依製造商料號關鍵字搜尋 BOM 料項（模糊比對）。</summary>
        public static List<QuotationItemData> GetItemsByManufacturerPartNo(string partNo)
        {
            QuotationDbRoot db = Load();
            string kw = partNo.Trim().ToLower();
            return db.QuotationItems
                .Where(i => i.ManufacturerPartNo1.ToLower().Contains(kw))
                .OrderBy(i => i.ManufacturerPartNo1)
                .ToList();
        }

        /// <summary>更新單筆料項的採購型號與建議採購型號旗標。</summary>
        public static bool SaveQuickItem(long id, string procurementModel, bool isRecommended)
        {
            QuotationDbRoot db = Load();
            QuotationItemData? item = db.QuotationItems.FirstOrDefault(i => i.Id == id);
            if (item == null) return false;

            item.ProcurementModel = procurementModel;
            item.IsRecommended = isRecommended;
            Save(db);
            return true;
        }

        #endregion
    }
}
