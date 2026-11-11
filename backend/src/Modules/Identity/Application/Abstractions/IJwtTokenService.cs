using Identity.Domain.Entities;
using System.Security.Claims;

namespace Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken();
    string HashToken(string rawToken);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}