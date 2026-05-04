using Microsoft.IdentityModel.Tokens;
using MySqlX.XDevAPI.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(Dictionary<string, string> userInfo)
    {
        var claims = new List<Claim>
        {
            new Claim("AccessToken", userInfo["AccessToken"]),
            new Claim("UserAccount", userInfo["UserAccount"]),
            new Claim("CompanyID", userInfo["CompanyID"]),
            new Claim("UserName", userInfo["UserName"]),
            new Claim("ExpireAt", userInfo["ExpireAt"]),
            new Claim("LoginType", "AD")
        };
        if (userInfo.TryGetValue("DeptList", out var deptList) && !string.IsNullOrEmpty(deptList))
        {
            claims.Add(new Claim("DeptList", deptList));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
