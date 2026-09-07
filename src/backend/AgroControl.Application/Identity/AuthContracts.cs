namespace AgroControl.Application.Identity;

public sealed record RegisterCommand(
    string OrganizationName,
    string DisplayName,
    string Email,
    string Password);

public sealed record LoginCommand(string Email, string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    Guid OrganizationId,
    string Role);

public sealed record AuthOperationResult(bool Succeeded, AuthResponse? Value, string? Error)
{
    public static AuthOperationResult Success(AuthResponse value) => new(true, value, null);
    public static AuthOperationResult Failure(string error) => new(false, null, error);
}
