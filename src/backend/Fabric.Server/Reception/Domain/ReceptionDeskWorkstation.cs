namespace Fabric.Server.Reception.Domain;

public sealed class ReceptionDeskWorkstation
{
    private ReceptionDeskWorkstation() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid LocationId { get; private set; }
    public string ApiKeyHash { get; private set; } = null!;
    public string ApiKeySalt { get; private set; } = null!;
    public bool Enabled { get; private set; }

    public static ReceptionDeskWorkstation Create(
        string name,
        Guid locationId,
        string apiKeyHash,
        string apiKeySalt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            LocationId = locationId,
            ApiKeyHash = apiKeyHash,
            ApiKeySalt = apiKeySalt,
            Enabled = true
        };

    public void Update(string name, Guid locationId, bool enabled)
    {
        Name = name;
        LocationId = locationId;
        Enabled = enabled;
    }

    public void RotateKey(string apiKeyHash, string apiKeySalt)
    {
        ApiKeyHash = apiKeyHash;
        ApiKeySalt = apiKeySalt;
    }

    public void Disable() => Enabled = false;
}
