using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Api.Ids;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Application.Holds.Commands.CancelHold;
using SchoolAdmission.Application.Holds.Commands.ConfirmHold;
using SchoolAdmission.Application.Holds.Commands.CreateHold;
using SchoolAdmission.Application.Holds.Queries.GetHoldById;
using SchoolAdmission.Application.Holds.Queries.GetMyHolds;

namespace SchoolAdmission.Api.Controllers;

/// <summary>Endpoints 4–8 of the contract: apply for a seat, read, confirm (pay), cancel, and list my applications.</summary>
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

    /// <summary>
    /// Pays for (confirms) the student's own HELD hold. 200 with the hold (same shape as GET; a repeat confirm returns it
    /// unchanged); 400 INVALID_STUDENT_ID, 404 HOLD_NOT_FOUND, 403 NOT_HOLD_OWNER, 409 HOLD_CANCELLED, 410 HOLD_EXPIRED.
    /// </summary>
    [HttpPost("holds/{holdId}/confirm")]
    [RequireStudentId]
    [ProducesResponseType<HoldResponse>(StatusCodes.Status200OK)]
    public async Task<HoldResponse> Confirm(
        string holdId, [FromServices] ConfirmHoldHandler handler, CancellationToken cancellationToken)
    {
        if (!HoldId.TryParse(holdId, out var id))
        {
            throw new HoldNotFoundException(holdId);
        }

        var confirmed = await handler.Handle(new ConfirmHoldCommand(id, HttpContext.GetStudentId(), holdId), cancellationToken);
        return HoldResponse.From(confirmed);
    }

    /// <summary>
    /// Cancels the student's own hold. 204 for HELD, and also (idempotent, no change) for EXPIRED or CANCELLED;
    /// 400 INVALID_STUDENT_ID, 404 HOLD_NOT_FOUND, 403 NOT_HOLD_OWNER, 409 HOLD_ALREADY_CONFIRMED.
    /// </summary>
    [HttpDelete("holds/{holdId}")]
    [RequireStudentId]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        string holdId, [FromServices] CancelHoldHandler handler, CancellationToken cancellationToken)
    {
        if (!HoldId.TryParse(holdId, out var id))
        {
            throw new HoldNotFoundException(holdId);
        }

        await handler.Handle(new CancelHoldCommand(id, HttpContext.GetStudentId(), holdId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Every application of the student in <c>X-Student-Id</c>, newest first, with effective status (an expired HELD reads
    /// EXPIRED). 200 with a bare array (empty when none); 400 INVALID_STUDENT_ID.
    /// </summary>
    [HttpGet("me/holds")]
    [RequireStudentId]
    [ProducesResponseType<IReadOnlyList<MyHoldResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<MyHoldResponse>> Mine([FromServices] GetMyHoldsHandler handler, CancellationToken cancellationToken)
    {
        var holds = await handler.Handle(new GetMyHoldsQuery(HttpContext.GetStudentId()), cancellationToken);
        return holds.Select(MyHoldResponse.From).ToList();
    }
}
