using SchoolAdmission.Api.Ids;
using SchoolAdmission.Application.Classes;

namespace SchoolAdmission.Api.Contracts;

/// <summary>
/// One item of <c>GET /classes</c>: exactly the contract fields. The list is a bare array, so it carries no
/// <c>serverTime</c> field; clients read the <c>X-Server-Time</c> header instead (ARCHITECTURE §8.3).
/// </summary>
public sealed record ClassListItemResponse(
    string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats)
{
    public static ClassListItemResponse From(ClassDto dto) => new(
        ClassId.Format(dto.Id), dto.Name, dto.TotalSeats, dto.HeldSeats, dto.ConfirmedSeats, dto.AvailableSeats);
}
