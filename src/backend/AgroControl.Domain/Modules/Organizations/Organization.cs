namespace AgroControl.Domain.Modules.Organizations;

public sealed class Organization
{
    private Organization() { }

    private Organization(Guid id, string name, string slug, DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static Organization Create(string name, string slug, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Organization slug is required.", nameof(slug));

        return new Organization(Guid.NewGuid(), name.Trim(), slug.Trim().ToLowerInvariant(), createdAtUtc);
    }
}
