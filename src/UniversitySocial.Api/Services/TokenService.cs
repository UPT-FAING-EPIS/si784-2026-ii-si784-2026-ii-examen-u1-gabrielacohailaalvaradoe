using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Services;

public interface ITokenService { string Create(User user); }

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string Create(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "UniversitySocial";
        var audience = configuration["Jwt:Audience"] ?? "UniversitySocial.Client";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
