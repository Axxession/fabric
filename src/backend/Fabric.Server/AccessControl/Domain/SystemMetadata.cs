using System.Text.Json.Serialization;

namespace Fabric.Server.AccessControl.Domain;

public sealed record SystemMetadataObject(string Id, string Name);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnipassMetadata), "unipass")]
public abstract record SystemMetadata;

public sealed record UnipassMetadata(
    IReadOnlyList<SystemMetadataObject> Sites,
    IReadOnlyList<SystemMetadataObject> AccessRules) : SystemMetadata;
