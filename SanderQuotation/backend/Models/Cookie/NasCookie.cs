namespace backend.Models.Cookie
{

    public static class NasCookieKey
    {
        public const string AccountFolder = "Nas_AccountFolder";

        public const string CompanyFolder = "Nas_CompanyFolder";

        public const string DeptFolder = "Nas_DeptFolder";

        public const string ProjectFolder = "Nas_ProjectFolder";
    }

    public class NasCookieValue
    {
        public string FolderType { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Sid { get; set; } = string.Empty;

        public string Account { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
