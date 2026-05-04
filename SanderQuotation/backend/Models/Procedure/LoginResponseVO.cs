namespace backend.Models.Procedure
{
    public class LoginResponseVO
    {
        public string CompanyID { get; set; } = string.Empty;
        public string UserAccount { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public NASDataVO NASData { get; set; } = new NASDataVO();
    }

    public class NASDataVO
    {
        public string NASCompanyType { get; set; } = string.Empty;
        public string NASCompanyUrl { get; set; } = string.Empty;
        public string NASCompanyPath { get; set; } = string.Empty;

        public string NASDepType { get; set; } = string.Empty;
        public string NASDepUrl { get; set; } = string.Empty;
        public string NASDepPath { get; set; } = string.Empty;

        public string NASAccountType { get; set; } = string.Empty;
        public string NASAccountUrl { get; set; } = string.Empty;
        public string NASAccountPath { get; set; } = string.Empty;

        public string NASProjectType { get; set; } = string.Empty;
        public string NASProjectUrl { get; set; } = string.Empty;
        public string NASProjectPath { get; set; } = string.Empty;
    }
}
