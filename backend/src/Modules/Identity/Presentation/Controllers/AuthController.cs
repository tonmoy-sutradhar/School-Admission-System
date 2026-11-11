using BuildingBlocks.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request, ct);
        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.LogoutAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Logged out."))
            : BadRequest(ApiResponse.Fail(result.Error.Message));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok("If the email exists, a reset token has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ResetPasswordAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Password reset successful."))
            : MapFailure(result.Error);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var result = await authService.VerifyEmailAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Email verified."))
            : MapFailure(result.Error);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ChangePasswordAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Password changed."))
            : MapFailure(result.Error);
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult GoogleReady() =>
        Ok(ApiResponse<GoogleAuthReadyResponse>.Ok(authService.GetGoogleAuthReadyInfo()));

    private IActionResult ToActionResult(Result<AuthResponse> result) =>
        result.IsSuccess
            ? Ok(ApiResponse<AuthResponse>.Ok(result.Value))
            : MapFailure(result.Error);

    private IActionResult MapFailure(Error error) => error.Code switch
    {
        "Unauthorized" => Unauthorized(ApiResponse.Fail(error.Message)),
        "Forbidden" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(error.Message)),
        "NotFound" => NotFound(ApiResponse.Fail(error.Message)),
        "Conflict" => Conflict(ApiResponse.Fail(error.Message)),
        _ => BadRequest(ApiResponse.Fail(error.Message))
    };
}