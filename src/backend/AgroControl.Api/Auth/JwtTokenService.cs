using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroControl.Application.Identity;
using AgroControl.Domain.Modules.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AgroControl.Api.Auth;

public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    public TokenResult Create(User user, OrganizationMembership membership)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(options.ExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("org_id", membership.OrganizationId.ToString()),
            new Claim(ClaimTypes.Role, membership.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
