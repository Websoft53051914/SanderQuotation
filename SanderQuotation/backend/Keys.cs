namespace backend
{
    public static class MESConfigKeys
    {
        // Company Info 相關
        public static class CompanyInfo
        {
            public const string ConfigType = "CompanyInfo";

            public const string CompanyName = "CompanyName";
            public const string CompanyShortName = "CompanyShortName";
            public const string CompanyAddress = "CompanyAddress";
            public const string CompanyPhone = "CompanyPhone";
            public const string CompanyLogoUrl = "CompanyLogoUrl";
            public const string CompanyEmail = "CompanyEmail";
        }

        // Nas Info 相關 (分類型態、路徑、網址)
        public static class NasInfo
        {
            public const string ConfigType = "NasInfo";

            public const string AccountFolderType = "AccountFolderType";
            public const string AccountFolderUrl = "AccountFolderUrl";
            public const string AccountFolderPath = "AccountFolderPath";

            public const string CompanyFolderType = "CompanyFolderType";
            public const string CompanyFolderUrl = "CompanyFolderUrl";
            public const string CompanyFolderPath = "CompanyFolderPath";

            public const string DepFolderType = "DepFolderType";
            public const string DepFolderUrl = "DepFolderUrl";
            public const string DepFolderPath = "DepFolderPath";

            public const string ProjectFolderType = "ProjectFolderType";
            public const string ProjectFolderUrl = "ProjectFolderUrl";
            public const string ProjectFolderPath = "ProjectFolderPath";
        }

        // System Info 相關
        public static class SystemInfo
        {
            public const string ConfigType = "SystemInfo";

            public const string SystemCode = "SystemCode";
            public const string SystemName = "SystemName";
            public const string SystemShortName = "SystemShortName";
            public const string SystemSupportEmail = "SystemSupportEmail";
            public const string SupportName = "SupportName";
            public const string SupportEmail = "SupportEmail";
            public const string SystemUrl = "SystemUrl";
            public const string LoginUrl = "LoginUrl";
            public const string ForgetPwdUrl = "ForgetPwdUrl";
            public const string DefaultLang = "DefaultLang";
            public const string ExpireHours = "ExpireHours";
            public const string DefaultPagesize = "DefaultPagesize";
        }
    }


    //public static class CookieKeys
    //{
    //    public const string AccountNasKey = "AccountNasKey";

    //    public const string DeptNasKey = "DeptNasKey";

    //    public const string CompanyNasKey = "CompanyNasKey";

    //    public const string ProjectNasKey = "ProjectNasKey";
    //}
}
