using DocumentFormat.OpenXml.Office2021.MipLabelMetaData;

namespace Business.DomainModel
{
    /// <summary>
    /// BOM 表內容
    /// </summary>
    public partial class BomFileContentDM : BaseDM
    {
        /// <summary>
        /// 元件料號
        /// </summary>
        public string? ComponentPart { get; set; }

        /// <summary>
        /// 元件描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public int? Qty { get; set; }

        /// <summary>
        /// 廠商
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// 廠商型號
        /// </summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>
        /// 顯示用料號
        /// </summary>
        public string? DisplayPart { get; set; }

        /// <summary>
        /// 檔案儲存代號(系統使用)
        /// </summary>
        public Guid UploadId { get; set; }
    }

    public partial class BomFileContentDM
    {
        /// <summary>
        /// tbbomfilequotation.Id
        /// </summary>
        public Guid? QuotationId { get; set; }

        /// <summary>
        /// 採購型號
        /// </summary>
        public string? No { get; set; }

        /// <summary>
        /// 內部採購單日期
        /// </summary>
        public DateTime? InternalPurchaseOrderDate { get; set; }

        /// <summary>
        /// 內部單價(原幣)
        /// </summary>
        public decimal? InternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 內部單價(台幣)
        /// </summary>
        public decimal? InternalUnitPriceTwd { get; set; }

        /// <summary>
        /// 內部採購數量
        /// </summary>
        public int? InternalQuantity { get; set; }

        /// <summary>
        /// 內部幣別
        /// </summary>
        public int? InternalCurrency { get; set; }

        /// <summary>
        /// 內部供應商名稱
        /// </summary>
        public int? InternalSupplierName { get; set; }

        /// <summary>
        /// 內部查價 AI 分群低價群最低價
        /// </summary>
        public decimal? InternalLowMinPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群低價群最高價
        /// </summary>
        public decimal? InternalLowMaxPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群高價群最低價
        /// </summary>
        public decimal? InternalHighMinPrice { get; set; }

        /// <summary>
        /// 內部查價 AI 分群高價群最高價
        /// </summary>
        public decimal? InternalHighMaxPrice { get; set; }

        /// <summary>
        /// 外部查價日期
        /// </summary>
        public DateTime? ExternalQuotationDate { get; set; }

        /// <summary>
        /// 外部單價(原幣)
        /// </summary>
        public decimal? ExternalUnitPriceOriginalCurrency { get; set; }

        /// <summary>
        /// 外部單價(台幣)
        /// </summary>
        public decimal? ExternalUnitPriceTwd { get; set; }

        /// <summary>
        /// 外部最小訂購量(MOQ)
        /// </summary>
        public int? ExternalMoq { get; set; }

        /// <summary>
        /// 外部幣別
        /// </summary>
        public int? ExternalCurrency { get; set; }

        /// <summary>
        /// 外部供應商名稱
        /// </summary>
        public string? ExternalSupplierName { get; set; }

        /// <summary>
        /// 是否為建議採購型號
        /// </summary>
        public bool IsRecommendedNo { get; set; }

        /// <summary>
        /// 是否套用 Variant 客戶承認料過濾
        /// </summary>
        public bool IsFilterByCustomerApprovedPart { get; set; }

        /// <summary>
        /// 內部查價所使用的客戶承認料清單（CSV 格式）
        /// </summary>
        public string? CustomerApprovedPartCsv { get; set; }
    }
}
