namespace backend.Models.Procedure
{
    public class DoLoadVO
    {
        public string Salt { get; set; } = string.Empty;

        public string LoadToken { get; set; }

        public List<DoLoadConfigData> ConfigData { get; set; } = new List<DoLoadConfigData>();

        public List<DoLoadCompanyData> CompanyData { get; set; } = new List<DoLoadCompanyData>();
    }

    public class DoLoadConfigData
    {

        public string ConfigType { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }

    public class DoLoadCompanyData
    {

        public string CompanyID { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }
}
