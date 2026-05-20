using System.Collections.Generic;

namespace Const.ApiModels.QueryPrice
{
    // ¢w¢w Token ¢w¢w
    public class DkTokenRspVO
    {
        public string access_token { get; set; } = "";
    }

    // ¢w¢w Keyword Search Request ¢w¢w
    public class DkKeywordSearchReqVO
    {
        public string Keywords { get; set; } = "";
        public int RecordCount { get; set; } = 20;
        public int RecordStartPosition { get; set; } = 0;
        public string MarketPlaceOptions { get; set; } = "IncludeMarketPlace";
    }

    // ¢w¢w Keyword Search Response ¢w¢w
    public class DkKeywordSearchRspVO
    {
        public List<DkProductVO> ExactMatches { get; set; } = new();
        public List<DkProductVO> Products { get; set; } = new();
    }

    // ¢w¢w Product Detail Response ¢w¢w
    public class DkProductDetailRspVO
    {
        public DkProductVO? Product { get; set; }
    }

    // ¢w¢w Product ¢w¢w
    public class DkProductVO
    {
        public string ManufacturerProductNumber { get; set; } = "";
        public DkManufacturerVO? Manufacturer { get; set; }
        public int QuantityAvailable { get; set; }
        public string? ProductUrl { get; set; }
        public string? DatasheetUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public List<DkProductVariationVO> ProductVariations { get; set; } = new();
    }

    public class DkManufacturerVO
    {
        public string Name { get; set; } = "";
    }

    // ¢w¢w ProductVariation ¢w¢w
    public class DkProductVariationVO
    {
        public string? DigiKeyProductNumber { get; set; }
        public int QuantityAvailableforPackageType { get; set; }
        public int MinimumOrderQuantity { get; set; }
        public bool MarketPlace { get; set; }
        public bool TariffActive { get; set; }
        public List<DkPriceBreakVO> StandardPricing { get; set; } = new();
        public List<DkPriceBreakVO> MyPricing { get; set; } = new();
    }

    // ¢w¢w Price Break ¢w¢w
    public class DkPriceBreakVO
    {
        public int BreakQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
