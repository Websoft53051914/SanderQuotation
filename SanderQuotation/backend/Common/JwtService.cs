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

    public string Generate(Dictionary<string, string> userInfo, out DateTime expiresUtc)
    {
        expiresUtc = DateTime.Now.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"]!));
        var claims = new List<Claim>
        {
            new Claim("UserAccount", userInfo["UserAccount"]),
            new Claim("UserName", userInfo["UserName"]),
            new Claim("PermissionCodeList", userInfo["PermissionCodeList"])
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Issuer"],
            claims: claims,
            expires: expiresUtc,
            signingCredentials: creds
        );


        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
