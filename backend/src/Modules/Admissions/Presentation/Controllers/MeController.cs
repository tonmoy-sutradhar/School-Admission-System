using Admissions.Application.Services;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Presentation.Controllers;

[ApiController]
[Route("me")]
public sealed class MeController(
    HoldService holdService,
    IStudentContext studentContext) : ControllerBase
{
    [HttpGet("holds")]
    public async Task<IActionResult> GetMyHolds(CancellationToken ct)
    {
        if (!studentContext.HasStudentId)
            return BadRequest(new { message = "X-Student-Id header is required (17-digit birth registration number)." });

        var result = await holdService.GetMyHoldsAsync(studentContext.StudentId!, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result.Error);
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
