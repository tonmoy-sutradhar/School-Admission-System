using FluentAssertions;
using Identity.Infrastructure.Security;

namespace UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_And_Verify_Should_Succeed_For_Same_Password()
    {
        var hash = _hasher.Hash("Password1!");
        _hasher.Verify("Password1!", hash).Should().BeTrue();
        _hasher.Verify("wrong", hash).Should().BeFalse();
    }
}