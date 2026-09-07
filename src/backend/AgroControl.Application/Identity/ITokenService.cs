using AgroControl.Domain.Modules.Identity;

namespace AgroControl.Application.Identity;

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenService
{
    TokenResult Create(User user, OrganizationMembership membership);
}
