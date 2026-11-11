using SchoolAdmission.Api.Ids;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Classes.Commands.CreateClass;

namespace SchoolAdmission.Api.Contracts;

/// <summary>
/// A single class as returned by <c>POST /classes</c> (and later <c>GET /classes/{id}</c>): the contract fields plus
/// the extra <c>serverTime</c> that object responses carry (ARCHITECTURE §8.3).
/// </summary>
public sealed record ClassResponse(
    string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats, string ServerTime)
{
    public static ClassResponse From(ClassDto dto) => new(
        ClassId.Format(dto.Id), dto.Name, dto.TotalSeats, dto.HeldSeats, dto.ConfirmedSeats, dto.AvailableSeats,
        ServerTimeMiddleware.Now());
}
