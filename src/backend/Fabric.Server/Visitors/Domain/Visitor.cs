public sealed class Visitor
{
    internal Visitor() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Company { get; private set; }
    public string? LicensePlate { get; private set; }

    public static Visitor Create(Guid id, Guid identityId, string firstName, string lastName, string email, string company, string? licensePlate = null)
    {
        return new Visitor
        {
            Id = id,
            IdentityId = identityId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Company = company,
            LicensePlate = licensePlate
        };
    }

    public void UpdateProfile(string firstName, string lastName, string email, string company, string? licensePlate = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Company = company;
        LicensePlate = licensePlate;
    }
}
