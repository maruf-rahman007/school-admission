using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Api.Ids;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Application.Holds.Commands.CreateHold;
using SchoolAdmission.Application.Holds.Queries.GetHoldById;

namespace SchoolAdmission.Api.Controllers;

/// <summary>Endpoints 4–5 of the contract: apply for a seat, read an application.</summary>
[ApiController]
public sealed class HoldsController : ControllerBase
{
    /// <summary>
    /// Holds a seat for the student in <c>X-Student-Id</c>. 201 with the hold and a Location header;
    /// 400 INVALID_STUDENT_ID, 404 CLASS_NOT_FOUND, 409 NO_SEATS_AVAILABLE or STUDENT_HAS_ACTIVE_HOLD. No request body.
    /// </summary>
    [HttpPost("classes/{classId}/holds")]
    [RequireStudentId]
    [ProducesResponseType<HoldCreatedResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        string classId, [FromServices] CreateHoldHandler handler, CancellationToken cancellationToken)
    {
        // An unparseable id is answered exactly like an unknown one.
        if (!ClassId.TryParse(classId, out var id))
        {
            throw new ClassNotFoundException(classId);
        }

        var created = await handler.Handle(
            new CreateHoldCommand(id, HttpContext.GetStudentId(), classId), cancellationToken);
        var response = HoldCreatedResponse.From(created);
        return Created($"/holds/{response.HoldId}", response);
    }

    /// <summary>Current state of one application; anyone may read it. Unknown or unparseable id → 404 HOLD_NOT_FOUND.</summary>
    [HttpGet("holds/{holdId}")]
    public async Task<HoldResponse> Get(
        string holdId, [FromServices] GetHoldByIdHandler handler, CancellationToken cancellationToken)
    {
        var found = HoldId.TryParse(holdId, out var id)
            ? await handler.Handle(new GetHoldByIdQuery(id), cancellationToken)
            : null;
        return HoldResponse.From(found ?? throw new HoldNotFoundException(holdId));
    }
}
