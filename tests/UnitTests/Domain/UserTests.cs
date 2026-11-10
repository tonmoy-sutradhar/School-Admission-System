using FluentAssertions;
using Identity.Domain.Entities;

namespace UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Register_Should_Create_User_With_Verification_Token()
    {
        var user = User.Register("Test@Email.com", "hash", "John", "Doe");

        user.Email.Should().Be("test@email.com");
        user.FirstName.Should().Be("John");
        user.EmailConfirmed.Should().BeFalse();
        user.EmailVerificationToken.Should().NotBeNullOrWhiteSpace();
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void ConfirmEmail_With_Valid_Token_Should_Confirm()
    {
        var user = User.Register("a@b.com", "hash", "A", "B");
        var token = user.EmailVerificationToken!;

        user.ConfirmEmail(token);

        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public void ConfirmEmail_With_Invalid_Token_Should_Throw()
    {
        var user = User.Register("a@b.com", "hash", "A", "B");
        var act = () => user.ConfirmEmail("bad-token");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RefreshToken_Rotation_Should_Revoke_Old_Token()
    {
        var user = User.Register("a@b.com", "hash", "A", "B");
        var token = user.AddRefreshToken("hash1", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        user.RevokeRefreshToken(token, "127.0.0.1", "Rotated", "hash2");
        user.AddRefreshToken("hash2", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        token.IsActive.Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("hash2");
        user.RefreshTokens.Count(t => t.IsActive).Should().Be(1);
    }

    [Fact]
    public void Deactivate_Should_Revoke_All_Refresh_Tokens()
    {
        var user = User.Register("a@b.com", "hash", "A", "B");
        user.AddRefreshToken("h1", DateTime.UtcNow.AddDays(1), null);
        user.AddRefreshToken("h2", DateTime.UtcNow.AddDays(1), null);

        user.Deactivate();

        user.IsActive.Should().BeFalse();
        user.RefreshTokens.Should().OnlyContain(t => !t.IsActive);
    }
}