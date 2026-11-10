using System.Security.Claims;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(AppClaimTypes.UserId)
                      ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
    public IEnumerable<string> Permissions => User?.FindAll(AppClaimTypes.Permission).Select(c => c.Value) ?? [];

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}