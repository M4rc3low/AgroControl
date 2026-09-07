using AgroControl.Application.Identity;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Organizations;
using Microsoft.EntityFrameworkCore;

namespace AgroControl.Infrastructure.Persistence;

public sealed class IdentityRepository(AgroControlDbContext dbContext) : IIdentityRepository
{
    public Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<OrganizationMembership?> GetPrimaryMembershipAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.OrganizationMemberships
            .OrderBy(membership => membership.CreatedAtUtc)
            .FirstOrDefaultAsync(membership => membership.UserId == userId, cancellationToken);

    public Task<Organization?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Organizations.SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken);

    public void Add(Organization organization) => dbContext.Organizations.Add(organization);
    public void Add(User user) => dbContext.Users.Add(user);
    public void Add(OrganizationMembership membership) => dbContext.OrganizationMemberships.Add(membership);
}
