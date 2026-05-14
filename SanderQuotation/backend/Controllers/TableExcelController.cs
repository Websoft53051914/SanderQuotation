using backend.Common.Attribute;
using Business.BusinessLogic;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Web.EX;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data.SqlClient;
using System.Globalization;
using ViewModel.TableExcel;

namespace backend.Controllers
{
    [Route("api/TableExcel")]
    public class TableExcelController : BaseProjectController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public TableExcelController(IConfiguration config, IWebHostEnvironment env):base(config)
        {
            _config = config;
            _env = env;
        }
        private readonly string _FileSaveFolder = "image";

        private TableExcelBL? tableExcelBL;
        private TableExcelBL GetTableExcelBL()
        {
            tableExcelBL ??= GetBLInstance<TableExcelBL>();
            return tableExcelBL;
        }

        // ── 取得分頁列表 ──────────────────────────────────────────────────

        /// <summary>
        /// 取得對應設定分頁列表
        /// </summary>
        [HttpGet("GetPageList")]
        [CustomAuthorization(Enums.FuncID.TableExcel_View)]
        public ActionResult GetPageList([FromQuery] ListPageEntity request,string Keyword)
        {
            try
            {
                var pageEntity = base.GetPageEntity(request);
                var res = GetTableExcelBL().GetPageList(pageEntity, new SearchVO { KeywordLike = Keyword });
                var vmList = res.Results.Select((dm, i) => new TableExcelGridVM
                {
                    No                      = (request.Page - 1) * request.PageSize + i + 1,
                    RowGuid                 = dm.Id,
                    TransferMappingCode     = dm.TransferMappingCode,
                    ExampleFileName         = dm.ExampleFileName,
                    ExampleFileTypeText     = ConvertFileTypeToText(dm.ExampleFileType),
                    SrcNasFilePath          = dm.SrcNasFilePath,
                    Description             = dm.Description,
                    MappingTables           = dm.MappingTables,
                    Status                  = dm.Status.ToString(),
                    CanDelete               = dm.EsScheduleCycleDMs.Count ==0
                }).ToList();

                return JsonSuccess(new
                {
                    Data     = vmList,
                    Total    = res.DataCount,
                    Page     = res.CurrentPage,
                    PageSize = res.PageDataSize,
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 新增設定 ──────────────────────────────────────────────────────

        /// <summary>
        /// 新增對應設定（同時寫入 EsFileTransferMapping + EsFileTransferMappingColumn）
        /// </summary>
        [HttpPost("Create")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Create)]
        public ActionResult Create([FromBody] TableExcelSettingVM vm)
        {
            if (vm == null)
                return JsonValidFail("請求格式錯誤，請確認 Content-Type 為 application/json 且資料格式正確");

            try
            {
                var ext = vm.ExampleFileType?.TrimStart('.') ?? "";
                var originalFileName = string.IsNullOrWhiteSpace(ext)
                    ? vm.ExampleFileName
                    : $"{vm.ExampleFileName}.{ext}";

                // 手動映射 VM 到 DM
                var dm = new EsFileTransferMappingDM
                {
                    TransferMappingCode = vm.TransferMappingCode,
                    ExampleFileName = vm.ExampleFileName,
                    ExampleFileType = ConvertFileTypeToInt(vm.ExampleFileType),
                    SrcNasFilePath = vm.SrcNasFilePath,
                    Description = vm.Description,
                    FileName = originalFileName,
                    Columns = vm.Sheets
                        .SelectMany(sheet =>
                            sheet.Mappings.Select(mapping => new EsFileTransferMappingColumnDM
                            {
                                SrcSheetName          = sheet.SrcSheetName,
                                SrcSheetIndex         = sheet.SrcSheetIndex,
                                HeaderRowIndex        = sheet.HeaderRowIndex,
                                DBTransferMappingCode     = sheet.DBName,

                                TargetTableName = sheet.TargetTableName,
                                SrcFileColumnName     = mapping.SrcFileColumnName,
                                TargetTableColumnName = mapping.TargetTableColumnName,
                                IsPrimaryKey          = mapping.IsPrimaryKey,
                                IsEncrypt             = mapping.IsEncrypt,
                                DefaultValue          = mapping.DefaultValue,
                                FilterCondition       = sheet.FilterCondition,
                                FilterMode            = sheet.FilterMode,
                            }))
                        .ToList()
                };

                GetTableExcelBL().CheckExist(dm);
                if (GetTableExcelBL().GetMessage().IsError())
                {
                    return JsonValidFail(GetTableExcelBL().GetMessage().GetErrMsg());
                }

                var transferCode = GetTableExcelBL().Insert(dm);

                // 將暫存的 Excel 檔案移至 wwwroot/image/{TransferMappingCode}/
                // 使用 BL 回傳的自動產生編號 (res.EntityID)，而非前端傳入的 vm.TransferMappingCode
                if (!string.IsNullOrWhiteSpace(vm.UploadedFilePath)
                    && !string.IsNullOrWhiteSpace(transferCode)
                    && System.IO.File.Exists(vm.UploadedFilePath))
                {
                    // 優先使用設定檔指定路徑（指向 EIP.frontend/wwwroot）
                    // 若未設定則自動推算同層的 EIP.frontend/wwwroot
                    var configuredPath = _config["FileStoragePath"];
                    var wwwRoot = !string.IsNullOrWhiteSpace(configuredPath)
                        ? configuredPath
                        : Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", Value.FileTransferDirectory, "wwwroot"));

                    var destDir = Path.Combine(wwwRoot, _FileSaveFolder, transferCode);
                    Directory.CreateDirectory(destDir);
                    var destPath = Path.Combine(destDir, originalFileName);
                    System.IO.File.Copy(vm.UploadedFilePath, destPath, overwrite: true);

                    // 複製完成後刪除暫存檔
                    try { System.IO.File.Delete(vm.UploadedFilePath); } catch { /* 暫存清除失敗不影響主流程 */ }
                }
                return JsonSuccess("新增成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 取得單筆資料（編輯用） ─────────────────────────────────────────

        /// <summary>
        /// 依 RowGuid 取得單筆對應設定（含所有 Sheet 欄位對應）
        /// </summary>
        [HttpGet("Get")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Edit)]
        public ActionResult Get([FromQuery] Guid id)
        {
            try
            {
                var dm = GetTableExcelBL().GetOne(id);
                if (dm == null)
                    return NotFound();

                var vm = new TableExcelSettingVM
                {
                    TransferMappingCode = dm.TransferMappingCode,
                    RowGuid         = dm.Id,
                    ExampleFileName = dm.ExampleFileName,
                    ExampleFileType = ConvertFileTypeToText(dm.ExampleFileType),
                    SrcNasFilePath  = dm.SrcNasFilePath,
                    Description     = dm.Description,
                    Sheets = dm.Columns
                        .GroupBy(c => c.SrcSheetIndex)
                        .OrderBy(g => g.Key)
                        .Select(g =>
                        {
                            var first = g.First();
                            return new TableExcelSheetVM
                            {
                                SrcSheetName   = first.SrcSheetName,
                                SrcSheetIndex  = first.SrcSheetIndex,
                                HeaderRowIndex = first.HeaderRowIndex,
                                DBName = first.DBTransferMappingCode,
                                TargetTableName = first.TargetTableName,
                                FilterCondition = first.FilterCondition,
                                FilterMode = first.FilterMode,

                                Mappings = g.Select(c => new TableExcelMappingVM
                                {
                                    SrcFileColumnName     = c.SrcFileColumnName,
                                    TargetTableColumnName = c.TargetTableColumnName,
                                    IsPrimaryKey          = c.IsPrimaryKey,
                                    IsEncrypt             = c.IsEncrypt,
                                    DefaultValue          = c.DefaultValue,
                                }).ToList()
                            };
                        })
                        .ToList()
                };

                return JsonSuccess(vm);
            }
            catch (Exception ex)
            {
                LogError(ex);
                Response.StatusCode = 500;
                return null;
            }
        }

        // ── 取得範本檔案 ──────────────────────────────────────────────────

        /// <summary>
        /// 依 RowGuid 取得新增時上傳的範本檔案
        /// </summary>
        [HttpGet("GetExampleFile")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Edit)]
        public IActionResult GetExampleFile([FromQuery] Guid id)
        {
            try
            {
                var dm = GetTableExcelBL().GetOne(id);
                if (dm == null || string.IsNullOrWhiteSpace(dm.FileName))
                    return NotFound();

                var configuredPath = _config["FileStoragePath"];
                var wwwRoot = !string.IsNullOrWhiteSpace(configuredPath)
                    ? configuredPath
                    : Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", Value.FileTransferDirectory, "wwwroot"));
              
                var filePath = Path.Combine(wwwRoot, _FileSaveFolder, dm.TransferMappingCode, dm.FileName);
                if (!System.IO.File.Exists(filePath))
                    return NotFound();

                var ext = Path.GetExtension(dm.FileName).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".xls"  => "application/vnd.ms-excel",
                    ".csv"  => "text/csv",
                    _       => "application/octet-stream"
                };

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, contentType, dm.FileName);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "SystemErrorMsg" + ex.Message));
            }
        }

        // ── 編輯設定 ──────────────────────────────────────────────────────

        /// <summary>
        /// 更新對應設定（同時重建 EsFileTransferMappingColumn，SheetName 變動亦一併套用）
        /// </summary>
        [HttpPost("Update")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Edit)]
        public ActionResult Update([FromBody] TableExcelSettingVM vm)
        {
            if (vm == null)
                return JsonValidFail("請求格式錯誤，請確認 Content-Type 為 application/json 且資料格式正確");

            try
            {
                var dm = new EsFileTransferMappingDM
                {
                    Id         = vm.RowGuid,
                    ExampleFileName = vm.ExampleFileName,
                    ExampleFileType = ConvertFileTypeToInt(vm.ExampleFileType),
                    SrcNasFilePath  = vm.SrcNasFilePath,
                    Description     = vm.Description,
                    Columns = vm.Sheets
                        .SelectMany(sheet =>
                            sheet.Mappings.Select(mapping => new EsFileTransferMappingColumnDM
                            {
                                SrcSheetName          = sheet.SrcSheetName,
                                SrcSheetIndex         = sheet.SrcSheetIndex,
                                HeaderRowIndex        = sheet.HeaderRowIndex,
                                DBTransferMappingCode   =sheet.DBName,

                                TargetTableName = sheet.TargetTableName,
                                SrcFileColumnName     = mapping.SrcFileColumnName,
                                TargetTableColumnName = mapping.TargetTableColumnName,
                                IsPrimaryKey          = mapping.IsPrimaryKey,
                                IsEncrypt             = mapping.IsEncrypt,
                                DefaultValue          = mapping.DefaultValue,
                                FilterCondition       = sheet.FilterCondition,
                                FilterMode            = sheet.FilterMode,
                            }))
                        .ToList()
                };

                GetTableExcelBL().Update(dm);
                return JsonSuccess("編輯成功");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 刪除設定 ─────────────────────────────────────────────────────
        /// <summary>
        /// 刪除對應設定（同時刪除 EsFileTransferMappingColumn + EsFileTransferMapping）
        /// </summary>
        [HttpPost("Delete")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Delete)]
        public ActionResult Delete([FromBody] List<string> ids)
        {
            try
            {
                // 刪除前先收集各筆記錄對應的實體檔案目錄路徑
                var configuredPath = _config["FileStoragePath"];
                var wwwRoot = !string.IsNullOrWhiteSpace(configuredPath)
                    ? configuredPath
                    : Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", Value.FileTransferDirectory, "wwwroot"));

                var dirsToDel = ids
                    .Select(id => Guid.TryParse(id, out var g) ? GetTableExcelBL().GetOne(g) : null)
                    .Where(dm => dm != null && !string.IsNullOrWhiteSpace(dm!.TransferMappingCode))
                    .Select(dm => Path.Combine(wwwRoot, _FileSaveFolder, dm!.TransferMappingCode))
                    .ToList();

                GetTableExcelBL().Delete(ids);
                // 刪除對應的實體檔案目錄
                foreach (var dir in dirsToDel)
                {
                    try
                    {
                        if (Directory.Exists(dir))
                            Directory.Delete(dir, recursive: true);
                    }
                    catch { /* 檔案清除失敗不影響主流程 */ }
                }
                return JsonSuccess("刪除成功");
            }
            catch (Exception ex)
            {
                return JsonValidFail(GetMsg(_config, "System_Error"));
            }
        }

        // ── 上傳 Excel ────────────────────────────────────────────────────

        /// <summary>
        /// 上傳 Excel 檔案，解析並回傳所有工作表及其第一列標題欄位
        /// </summary>
        [HttpPost("UploadExcel")]
        [Consumes("multipart/form-data")]
        [CustomAuthorization(Enums.FuncID.TableExcel_Create, Enums.FuncID.TableExcel_Edit)]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return JsonValidFail(GetMsg(_config, "Upload_Please_Select_File"));

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".xls")
                    return JsonValidFail(GetMsg(_config, "Upload_Excel_Format_Only"));

                // 儲存至伺服器暫存目錄
                var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "Excel");
                Directory.CreateDirectory(uploadDir);

                var savedFileName = $"{Guid.NewGuid()}{ext}";
                var savedFilePath = Path.Combine(uploadDir, savedFileName);

                await using (var fs = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                var sheets = ParseExcelSheets(savedFilePath, ext);

                return JsonSuccess(new UploadExcelResultVM
                {
                    FilePath = savedFilePath,
                    Sheets = sheets
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "Upload_Failed") + "：" + ex.Message);
            }
        }

        // ── 取得資料表清單 ─────────────────────────────────────────────────

        /// <summary>
        /// 取得目的資料庫所有資料表名稱（供下拉選單）
        /// </summary>
        [HttpGet("GetTableList")]
        public IActionResult GetTableList()
        {
            try
            {
                var connStr = _config.GetConnectionString("MainConnection");
                if (string.IsNullOrWhiteSpace(connStr))
                    return JsonValidFail("MainConnection 連線字串未設定");

                var tables = QueryTableNames(connStr);
                return JsonSuccess(tables);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "SystemErrorMsg" + ex.Message));
            }
        }

        // ── 取得資料表欄位 ─────────────────────────────────────────────────

        /// <summary>
        /// 取得指定資料表的欄位名稱清單（供下拉選單）
        /// </summary>
        [HttpGet("GetTableColumns")]
        public IActionResult GetTableColumns([FromQuery] string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return JsonValidFail("請輸入資料表名稱");

                var connStr = _config.GetConnectionString("MainConnection");
                if (string.IsNullOrWhiteSpace(connStr))
                    return JsonValidFail("MainConnection 連線字串未設定");

                var columns = QueryColumnNames(connStr, tableName);
                return JsonSuccess(columns);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "SystemErrorMsg" + ex.Message));
            }
        }

        // ── 依伺服器路徑取得 Excel 欄位（編輯模式補載用） ──────────────────

        /// <summary>
        /// 依伺服器端儲存的 Excel 路徑，取得第一個工作表的欄位名稱清單
        /// </summary>
        [HttpGet("GetExcelColumns")]
        public IActionResult GetExcelColumns([FromQuery] string filePath, [FromQuery] int headerRowIndex = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                    return JsonSuccess(new List<string>());

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var columns = ext == ".csv"
                    ? ParseCsvColumns(filePath, headerRowIndex)
                    : ParseExcelSheetColumns(filePath, ext, 0, headerRowIndex);
                return JsonSuccess(columns);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "SystemErrorMsg" + ex.Message));
            }
        }

        // ── 依伺服器路徑取得指定工作表欄位（支援自訂標題行） ─────────────────

        /// <summary>
        /// 依檔案路徑、工作表索引與標題起始行（1-based），取得欄位名稱清單
        /// </summary>
        [HttpGet("GetSheetColumns")]
        public IActionResult GetSheetColumns([FromQuery] string filePath, [FromQuery] int sheetIndex = 0, [FromQuery] int headerRowIndex = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                    return JsonSuccess(new List<string>());

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var columns = ext == ".csv"
                    ? ParseCsvColumns(filePath, headerRowIndex)
                    : ParseExcelSheetColumns(filePath, ext, sheetIndex, headerRowIndex);
                return JsonSuccess(columns);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return JsonValidFail(GetMsg(_config, "SystemErrorMsg" + ex.Message));
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        /// <summary>
        /// 解析 Excel 或 CSV 檔案，以第一列作為欄位標題，回傳所有 Sheet 資訊
        /// </summary>
        private static List<ExcelSheetInfoVM> ParseExcelSheets(string filePath, string ext)
        {
            if (ext == ".csv")
            {
                var sheetName = Path.GetFileNameWithoutExtension(filePath);
                var previewColumns = ParseCsvColumns(filePath, 1);
                return new List<ExcelSheetInfoVM>
                {
                    new ExcelSheetInfoVM
                    {
                        Name = sheetName,
                        ColumnCount = previewColumns.Count,
                        Columns = previewColumns
                    }
                };
            }

            var result = new List<ExcelSheetInfoVM>();

            using var stream = System.IO.File.OpenRead(filePath);
            IWorkbook workbook = ext == ".xlsx"
                ? (IWorkbook)new XSSFWorkbook(stream)
                : new HSSFWorkbook(stream);

            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                var headerRow = sheet.GetRow(0);
                var columns = new List<string>();

                if (headerRow != null)
                {
                    for (int j = 0; j < headerRow.LastCellNum; j++)
                    {
                        var cell = headerRow.GetCell(j);
                        var cellValue = cell?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(cellValue))
                            columns.Add(cellValue);
                    }
                }

                result.Add(new ExcelSheetInfoVM
                {
                    Name = sheet.SheetName,
                    ColumnCount = columns.Count,
                    Columns = columns
                });
            }

            return result;
        }

        private static List<string> ParseCsvColumns(string filePath, int headerRowIndex)
        {
            var rowIdx = Math.Max(headerRowIndex - 1, 0);
            var lines = System.IO.File.ReadAllLines(filePath);
            if (rowIdx >= lines.Length) return new List<string>();
            return lines[rowIdx].Split(',')
                .Select(c => c.Trim().Trim('"'))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
        }

        private static List<string> ParseExcelSheetColumns(string filePath, string ext, int sheetIndex, int headerRowIndex)
        {
            var rowIdx = Math.Max(headerRowIndex - 1, 0);
            using var stream = System.IO.File.OpenRead(filePath);
            IWorkbook workbook = ext == ".xlsx"
                ? (IWorkbook)new XSSFWorkbook(stream)
                : new HSSFWorkbook(stream);
            if (sheetIndex >= workbook.NumberOfSheets) return new List<string>();
            var sheet = workbook.GetSheetAt(sheetIndex);
            var headerRow = sheet.GetRow(rowIdx);
            var columns = new List<string>();
            if (headerRow != null)
            {
                for (int j = 0; j < headerRow.LastCellNum; j++)
                {
                    var cell = headerRow.GetCell(j);
                    var cellValue = cell?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                        columns.Add(cellValue);
                }
            }
            return columns;
        }

        private static List<string> QueryTableNames(string connStr)
        {
            var tables = new List<string>();
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                tables.Add(reader.GetString(0));
            return tables;
        }

        private static List<string> QueryColumnNames(string connStr, string tableName)
        {
            var columns = new List<string>();
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY ORDINAL_POSITION";
            cmd.Parameters.AddWithValue("@TableName", tableName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(0));
            return columns;
        }

        /// <summary>
        /// 將檔案類型字串轉換為整數
        /// </summary>
        private static int ConvertFileTypeToInt(string fileType)
        {
            return fileType?.ToLowerInvariant() switch
            {
                ".xlsx" or "xlsx" => (int)Enums.TransferFileTypeEnum.Xlsx,
                ".xls" or "xls"   => (int)Enums.TransferFileTypeEnum.Xls,
                ".csv" or "csv"   => (int)Enums.TransferFileTypeEnum.Csv,
                _                 => (int)Enums.TransferFileTypeEnum.Unknown
            };
        }

        /// <summary>
        /// 將檔案類型整數轉換為文字
        /// </summary>
        private static string ConvertFileTypeToText(int fileType)
        {
            return (Enums.TransferFileTypeEnum)fileType switch
            {
                Enums.TransferFileTypeEnum.Xlsx => "xlsx",
                Enums.TransferFileTypeEnum.Xls  => "xls",
                Enums.TransferFileTypeEnum.Csv  => "csv",
                _                               => ""
            };
        }
    }
}
