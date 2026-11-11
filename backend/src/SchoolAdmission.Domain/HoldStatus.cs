namespace SchoolAdmission.Domain;

/// <summary>Lifecycle of a seat hold. HELD and CONFIRMED are "active": they take a seat and block new applications.</summary>
public enum HoldStatus
{
    Held,
    Confirmed,
    Expired,
    Cancelled,
}
