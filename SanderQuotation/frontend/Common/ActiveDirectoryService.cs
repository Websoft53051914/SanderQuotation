using System;
using System.DirectoryServices.Protocols;
using System.Net;

public class ActiveDirectoryService
{
    private readonly string _ldapServer;
    private readonly int _ldapPort;
    private readonly string _domain;

    public ActiveDirectoryService(string ldapServer, int ldapPort = 389, string domain = "")
    {
        _ldapServer = ldapServer;
        _ldapPort = ldapPort;
        _domain = domain;
    }

    public bool ValidateUser(string username, string password)
    {
        try
        {
            // 帳號加上 Domain
            string userDn = string.IsNullOrEmpty(_domain) ? username : $"{_domain}\\{username}";

            using (var ldap = new LdapConnection(new LdapDirectoryIdentifier(_ldapServer, _ldapPort)))
            {
                var credential = new NetworkCredential(userDn, password);
                ldap.AuthType = AuthType.Negotiate;
                ldap.Bind(credential);  // 嘗試登入

                // 登入成功
                return true;
            }
        }
        catch (LdapException)
        {
            // 驗證失敗
            return false;
        }
        catch (Exception ex)
        {
            // 其他例外
            throw new Exception($"AD驗證發生錯誤: {ex.Message}");
        }
    }
}
