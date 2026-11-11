using SchoolAdmission.Application.Holds;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Api.Contracts;

/// <summary>
/// One element of <c>GET /me/holds</c>: exactly the contract fields. The array itself is the response, so the server
/// time travels only in the <c>X-Server-Time</c> header.
/// </summary>
public sealed record MyHoldResponse(string HoldId, string ClassId, string ClassName, string Status, string ExpiresAt)
{
    public static MyHoldResponse From(HoldDto dto) => new(
        Ids.HoldId.Format(dto.Id), Ids.ClassId.Format(dto.ClassId), dto.ClassName, HoldStatusText.ToText(dto.Status),
        ApiTime.Format(dto.ExpiresAt));
}
