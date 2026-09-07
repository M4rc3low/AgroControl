using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Organizations;

namespace AgroControl.Application.Identity;

public interface IIdentityRepository
{
    Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<OrganizationMembership?> GetPrimaryMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Organization?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    void Add(Organization organization);
    void Add(User user);
    void Add(OrganizationMembership membership);
}
