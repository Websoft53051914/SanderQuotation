
namespace ViewModel
{

    public class GeoIP_City_IP2LocationLiteVM 
    {

		public long IP_FromNum { set; get; }
		
		public long IP_ToNum { set; get; }
		
		public string CountryCode { set; get; }
		
		public string CountryName { set; get; }
		
		public string RegionName { set; get; }
		
		public string CityName { set; get; }
		
		public decimal? Latitude { set; get; }
		
		public decimal? Longitude { set; get; }
		
		public string ZipCode { set; get; }
		
		public string TimeZone { set; get; }
		

    }

}
