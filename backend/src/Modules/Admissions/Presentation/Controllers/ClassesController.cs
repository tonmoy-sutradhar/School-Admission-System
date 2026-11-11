using Admissions.Application.DTOs;
using Admissions.Application.Services;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Admissions.Presentation.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController(
    ClassService classService,
    HoldService holdService,
    IStudentContext studentContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        var result = await classService.CreateAsync(request, ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToError(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await classService.ListAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result.Error);
    }

    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetById(Guid classId, CancellationToken ct)
    {
        var result = await classService.GetByIdAsync(classId, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result.Error);
    }

    [HttpPost("{classId:guid}/holds")]
    public async Task<IActionResult> CreateHold(Guid classId, CancellationToken ct)
    {
        if (!studentContext.HasStudentId)
            return BadRequest(new { message = "X-Student-Id header is required (17-digit birth registration number)." });

        var result = await holdService.CreateHoldAsync(classId, studentContext.StudentId!, ct);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToError(result.Error);
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
