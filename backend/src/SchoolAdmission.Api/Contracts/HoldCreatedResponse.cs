using SchoolAdmission.Api.Ids;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Holds.Commands.CreateHold;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Api.Contracts;

/// <summary>201 body of <c>POST /classes/{classId}/holds</c>: the contract fields plus <c>serverTime</c> (ARCHITECTURE §8.3).</summary>
public sealed record HoldCreatedResponse(
    string HoldId, string ClassId, string StudentId, string Status, string ExpiresAt, string ServerTime)
{
    public static HoldCreatedResponse From(HoldCreatedDto dto) => new(
        Ids.HoldId.Format(dto.HoldId), Ids.ClassId.Format(dto.ClassId), dto.StudentId, HoldStatusText.ToText(dto.Status),
        ApiTime.Format(dto.ExpiresAt), ServerTimeMiddleware.Now());
}
