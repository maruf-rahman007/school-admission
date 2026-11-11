using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Ids;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Classes.Commands.CreateClass;
using SchoolAdmission.Application.Classes.Queries.GetClassById;
using SchoolAdmission.Application.Classes.Queries.GetClasses;

namespace SchoolAdmission.Api.Controllers;

/// <summary>Endpoints 1–3 of the contract. No X-Student-Id is needed here.</summary>
[ApiController]
[Route("classes")]
public sealed class ClassesController : ControllerBase
{
    /// <summary>Creates a class (school admin). 201 with the class and a Location header.</summary>
    [HttpPost]
    [ProducesResponseType<ClassResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateClassRequest request, [FromServices] CreateClassHandler handler, CancellationToken cancellationToken)
    {
        var created = await handler.Handle(new CreateClassCommand(request.Name, request.TotalSeats), cancellationToken);
        var response = ClassResponse.From(created);
        return Created($"/classes/{response.Id}", response);
    }

    /// <summary>All classes with seat counts, sorted by name. A bare array (server time is in the header).</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ClassListItemResponse>> List(
        [FromServices] GetClassesHandler handler, CancellationToken cancellationToken)
    {
        var classes = await handler.Handle(new GetClassesQuery(), cancellationToken);
        return [.. classes.Select(ClassListItemResponse.From)];
    }

    /// <summary>One class with seat counts. Unknown or unparseable id → 404 CLASS_NOT_FOUND.</summary>
    [HttpGet("{classId}")]
    public async Task<ClassResponse> Get(
        string classId, [FromServices] GetClassByIdHandler handler, CancellationToken cancellationToken)
    {
        // The only 404 path: an id that can't be parsed is answered exactly like one that doesn't exist.
        var found = ClassId.TryParse(classId, out var id)
            ? await handler.Handle(new GetClassByIdQuery(id), cancellationToken)
            : null;
        return ClassResponse.From(found ?? throw new ClassNotFoundException(classId));
    }
}
