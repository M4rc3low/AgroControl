using System.Globalization;
using System.Text;
using AgroControl.Application.Common;
using AgroControl.Application.Subscriptions;
using AgroControl.Application.RegionalOperations;
using AgroControl.Domain.Modules.Identity;
using AgroControl.Domain.Modules.Organizations;
using AgroControl.Domain.Modules.Operations;
using AgroControl.Domain.Modules.Subscriptions;

namespace AgroControl.Application.Identity;

public sealed class AuthService
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IMultiFarmRepository _multiFarmRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IIdentityRepository identityRepository,
        ISubscriptionRepository subscriptionRepository,
        IMultiFarmRepository multiFarmRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _subscriptionRepository = subscriptionRepository;
        _multiFarmRepository = multiFarmRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.OrganizationName) ||
            string.IsNullOrWhiteSpace(command.DisplayName) ||
            string.IsNullOrWhiteSpace(command.Email))
        {
            return AuthOperationResult.Failure("organizationName, displayName and email are required.");
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
            return AuthOperationResult.Failure("Password must contain at least 8 characters.");

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var existingUser = await _identityRepository.FindUserByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
            return AuthOperationResult.Failure("An account with this email already exists.");

        var now = DateTime.UtcNow;
        var organization = Organization.Create(
            command.OrganizationName,
            BuildSlug(command.OrganizationName),
            now);

        var user = User.Create(
            normalizedEmail,
            command.DisplayName,
            _passwordHasher.Hash(command.Password),
            now);

        var membership = OrganizationMembership.Create(
            organization.Id,
            user.Id,
            OrganizationRole.Owner,
            now);

        var subscription = Subscription.CreateBasic(organization.Id, now);
        var allFarmsAccess = FarmAccessAssignment.CreateAllFarms(organization.Id, user.Id, user.Id, now);

        _identityRepository.Add(organization);
        _identityRepository.Add(user);
        _identityRepository.Add(membership);
        _subscriptionRepository.Add(subscription);
        _multiFarmRepository.AddAssignment(allFarmsAccess);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success(ToResponse(user, membership));
    }

    public async Task<AuthOperationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await _identityRepository.FindUserByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(command.Password, user.PasswordHash))
            return AuthOperationResult.Failure("Invalid email or password.");

        var membership = await _identityRepository.GetPrimaryMembershipAsync(user.Id, cancellationToken);
        if (membership is null)
            return AuthOperationResult.Failure("The user is not linked to an organization.");

        return AuthOperationResult.Success(ToResponse(user, membership));
    }

    private AuthResponse ToResponse(User user, OrganizationMembership membership)
    {
        var token = _tokenService.Create(user, membership);
        return new AuthResponse(
            token.AccessToken,
            token.ExpiresAtUtc,
            user.Id,
            membership.OrganizationId,
            membership.Role.ToString());
    }

    private static string BuildSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
                builder.Append(character);
            else if ((char.IsWhiteSpace(character) || character == '-') && builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "organization";

        const int suffixLength = 9; // '-' + 8 hex chars
        const int maxSlugLength = 180;
        var maxBaseLength = maxSlugLength - suffixLength;
        if (slug.Length > maxBaseLength)
            slug = slug[..maxBaseLength].TrimEnd('-');

        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{slug}-{suffix}";
    }
}
