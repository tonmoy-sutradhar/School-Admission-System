using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll(AppClaimTypes.Permission).Select(c => c.Value);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}