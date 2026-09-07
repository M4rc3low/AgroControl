namespace AgroControl.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "AgroControl";
    public string Audience { get; init; } = "AgroControl.Web";
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
