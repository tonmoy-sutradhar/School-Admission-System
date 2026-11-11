using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Roles.Application.DTOs;
using Roles.Application.Services;
using Users.Application.DTOs;
using Users.Application.Services;

namespace Users.Infrastructure.Services;

public sealed class UserProvisioningService(
    UserService userService,
    IServiceScopeFactory scopeFactory) : IUserProvisioningService
{
    public async Task ProvisionNewUserAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        await userService.EnsureProfileAsync(
            new CreateUserProfileMessage(userId, email, firstName, lastName),
            cancellationToken);

        // Resolve RoleService from a scope to avoid circular DI at composition root
        using var scope = scopeFactory.CreateScope();
        var roleService = scope.ServiceProvider.GetRequiredService<RoleService>();
        await roleService.AssignRolesAsync(
            userId,
            new AssignRolesRequest([SystemRoles.User]),
            cancellationToken);
    }
}