using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Api.Contracts;

/// <summary>
/// <c>GET /holds/{holdId}</c>: the contract fields plus the extra <c>className</c> (so the application page needs one
/// request) and <c>serverTime</c>.
/// </summary>
public sealed record HoldResponse(
    string HoldId, string ClassId, string Status, string ExpiresAt, string ClassName, string ServerTime)
{
    public static HoldResponse From(HoldDto dto) => new(
        Ids.HoldId.Format(dto.Id), Ids.ClassId.Format(dto.ClassId), HoldStatusText.ToText(dto.Status),
        ApiTime.Format(dto.ExpiresAt), dto.ClassName, ServerTimeMiddleware.Now());
}
