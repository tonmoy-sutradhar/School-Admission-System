using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Common.Models;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roles.Application.DTOs;
using Roles.Application.Services;

namespace Roles.Presentation.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public sealed class RolesController(RoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await roleService.GetRolesAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(result.Value));
    }

    [HttpGet("permissions")]
    [HasPermission(Permissions.RolesView)]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var result = await roleService.GetPermissionsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(result.Value));
    }

    [HttpPost]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.Ok(result.Value))
            : Conflict(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.Ok(result.Value))
            : BadRequest(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPost("users/{userId:guid}/assign")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignRolesRequest request, CancellationToken ct)
    {
        var result = await roleService.AssignRolesAsync(userId, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Roles assigned."))
            : BadRequest(ApiResponse.Fail(result.Error.Message));
    }
}