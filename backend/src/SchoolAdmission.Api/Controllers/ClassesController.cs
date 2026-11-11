using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Application.Classes.Commands.CreateClass;

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
}
