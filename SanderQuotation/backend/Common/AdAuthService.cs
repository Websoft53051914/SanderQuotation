using System.DirectoryServices.Protocols;
using System.Net;

public class AdAuthService
{
    private readonly IConfiguration _config;

    public AdAuthService(IConfiguration config)
    {
        _config = config;
    }

    public class ADInfo
    {
        public string Server { get; set; }
        public int Port { get; set; } = 389;
        public string Domain { get; set; }
        public string BaseDN { get; set; }
    }

    public bool Validate(ADInfo adInfo, string username, string password, out Dictionary<string, string> userInfo, out string errorMsg)
    {
        userInfo = new();

        try
        {
            var identifier = new LdapDirectoryIdentifier(adInfo.Server, adInfo.Port);
            using var connection = new LdapConnection(identifier)
            {
                AuthType = AuthType.Negotiate,
                Credential = new NetworkCredential(username, password, adInfo.Domain)
            };

            // 🔐 驗證帳密（Bind）
            connection.Bind();

            string domainOrUpn = adInfo.Domain; // e.g. MUPOC or mupoc.local
            string baseDn;

            if (domainOrUpn.Contains("."))
            {
                // UPN domain
                baseDn = string.Join(",", domainOrUpn.Split('.').Select(p => $"DC={p}"));
            }
            else
            {
                // NetBIOS domain，不知道 DC，最好在配置中加 BaseDN
                baseDn = adInfo.BaseDN;
            }

            // 🔍 查詢登入者資料
            var request = new SearchRequest(
                baseDn,
                $"(&(objectClass=user)(sAMAccountName={username}))",
                SearchScope.Subtree,
                new[] { "displayName", "mail", "memberOf" }
            );

            var response = (SearchResponse)connection.SendRequest(request);

            var entry = response.Entries[0];
            userInfo["Name"] = entry.Attributes["displayName"]?[0]?.ToString() ?? username;
            userInfo["Email"] = entry.Attributes["mail"]?[0]?.ToString() ?? "";
            errorMsg = "";
            return true;
        }
        catch (Exception ex)
        {
            errorMsg = ex.ToString();
            return false;
        }
    }
}
