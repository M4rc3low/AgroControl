using AgroControl.Infrastructure.Security;

namespace AgroControl.UnitTests;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_and_verify_round_trip()
    {
        const string password = "AgroControl#2026";
        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
        Assert.False(_hasher.Verify("wrong-password", hash));
    }
}
