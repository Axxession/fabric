namespace Fabric.Hardware.BelgianEid;

public sealed record BelgianEidIdentity(
    string FirstName,
    string LastName,
    string NationalNumber,
    string DocumentNumber,
    DateOnly? ExpiryDate,
    string? Nationality,
    string? BirthLocation,
    string? BirthDateRaw);
