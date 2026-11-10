using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Common.Models;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Application.DTOs;
using Users.Application.Services;

namespace Users.Presentation.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await userService.GetMeAsync(ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserProfileDto>.Ok(result.Value))
            : Unauthorized(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await userService.UpdateMeAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserProfileDto>.Ok(result.Value))
            : BadRequest(ApiResponse.Fail(result.Error.Message));
    }

    [HttpGet("{userId:guid}")]
    [HasPermission(Permissions.UsersView)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(userId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserProfileDto>.Ok(result.Value))
            : NotFound(ApiResponse.Fail(result.Error.Message));
    }

    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await userService.GetPagedAsync(request, ct);
        return Ok(ApiResponse<PagedResult<UserProfileDto>>.Ok(result.Value));
    }

    [HttpPost("{userId:guid}/activate")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        var result = await userService.SetActiveAsync(userId, true, ct);
        return result.IsSuccess ? Ok(ApiResponse.Ok("User activated.")) : NotFound(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPost("{userId:guid}/deactivate")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        var result = await userService.SetActiveAsync(userId, false, ct);
        return result.IsSuccess ? Ok(ApiResponse.Ok("User deactivated.")) : NotFound(ApiResponse.Fail(result.Error.Message));
    }
}