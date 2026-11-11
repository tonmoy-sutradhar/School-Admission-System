using Admissions.Application.Services;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Presentation.Controllers;

[ApiController]
[Route("holds")]
public sealed class HoldsController(
    HoldService holdService,
    IStudentContext studentContext) : ControllerBase
{
    [HttpGet("{holdId:guid}")]
    public async Task<IActionResult> Get(Guid holdId, CancellationToken ct)
    {
        var result = await holdService.GetHoldAsync(holdId, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result.Error);
    }

    [HttpPost("{holdId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid holdId, CancellationToken ct)
    {
        if (!studentContext.HasStudentId)
            return BadRequest(new { message = "X-Student-Id header is required (17-digit birth registration number)." });

        var result = await holdService.ConfirmAsync(holdId, studentContext.StudentId!, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result.Error);
    }

    [HttpDelete("{holdId:guid}")]
    public async Task<IActionResult> Cancel(Guid holdId, CancellationToken ct)
    {
        if (!studentContext.HasStudentId)
            return BadRequest(new { message = "X-Student-Id header is required (17-digit birth registration number)." });

        var result = await holdService.CancelAsync(holdId, studentContext.StudentId!, ct);
        if (result.IsSuccess)
            return NoContent();

        return ToError(result.Error);
    }

    private ObjectResult ToError(Error error) => error.Code switch
    {
        "NotFound" => NotFound(new { message = error.Message }),
        "Conflict" => Conflict(new { message = error.Message }),
        "Forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { message = error.Message }),
        "Unauthorized" => Unauthorized(new { message = error.Message }),
        _ => BadRequest(new { message = error.Message })
    };
}
