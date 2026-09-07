namespace AgroControl.Domain.Modules.Identity;

public sealed class User
{
    private User() { }

    private User(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new User(
            Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            displayName.Trim(),
            passwordHash,
            createdAtUtc);
    }
}
