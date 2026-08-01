# DTOs, Mapping, and Enums

## DTO Naming

Drop the `Dto` suffix. Use `Request` and `Response` as the default.

- **`{Verb}{Noun}Request`** — inbound API shape. Always include the verb.
- **`{Noun}Response`** — full outbound shape, single resource or list when full shape.
- **`{Noun}SummaryResponse`** — reduced outbound shape. Use only when the list returns a subset.
- **`Dto`** — reserved for genuinely shared transfer objects that are neither input nor output (rare).

```
MyService.Api/
└── Dtos/
    ├── VisitResponse.cs
    ├── VisitSummaryResponse.cs
    ├── ScheduleVisitRequest.cs
    └── CancelVisitRequest.cs
```

## Mapping Library: Mapperly

Use **Mapperly** as the default mapping library. It generates mapping code at compile time — incomplete or incompatible mappings fail the build, not production.

```csharp
[Mapper]
public static partial class VisitMapper
{
    public static partial VisitResponse ToResponse(this Visit visit);
}
```

**Rules:**
- Mappers are `static partial` classes — no instance, no DI registration.
- Extension method style (`this Visit visit`).
- If a mapping needs a service to complete, that logic belongs in the handler, not the mapper.

### Where mappers live

| Direction | Location |
|---|---|
| Domain → `Response` | `.Api` project — co-located with the response type or sibling `*Mapper.cs` |
| `Request` → Domain | Server project — co-located with the endpoint or `Mapping/` folder |

Co-location is preferred. A dedicated `Mapping/` folder is acceptable when volume makes co-location noisy.

## Polymorphic Request and Response Types

Model as an `abstract record` base with `sealed record` derived types:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateEtherNetReaderRequest), "ethernet")]
[JsonDerivedType(typeof(CreateRs485ReaderRequest), "rs485")]
public abstract record CreateReaderRequest
{
    public required string Name { get; init; }
}

public sealed record CreateEtherNetReaderRequest : CreateReaderRequest
{
    public required string IpAddress { get; init; }
    public required string PortNumber { get; init; }
}
```

**Rules:**
- Discriminator property name is always `"type"` (lowercase).
- Discriminator values are lowercase snake_case.
- `abstract record` carries shared properties; `sealed record` carries subtype-specific properties.

## Enums

### Enums belong in the `.Api` project

Any enum exposed through HTTP, accepted in a request, or shared across service boundaries lives in the `.Api` project. The server project defines its own domain enums with identical values and casts at the boundary.

```
MyService.Api/Models/JobStatus.cs         — public enum JobStatus { Planned, Active, Completed, Cancelled }
MyService.Server/Domain/Job/JobStatus.cs  — internal enum JobStatus { Planned, Active, Completed, Cancelled }
```

Cast at the endpoint boundary — never import the domain enum into `.Api` or vice versa:

```csharp
Status = (Api.Models.JobStatus)job.Status,
```

Mapperly handles cross-namespace enum mapping automatically when values are identical.

### Enum serialization: strings always

**All enums serialise as strings.** JSON representations must be human-readable strings — never integers.

**`[Flags]` enums are the only exception.** They serialise as integers.

```csharp
public enum JobStatus { Planned, Active, Completed, Cancelled }

[Flags]
public enum FilePermissions { None = 0, Read = 1, Write = 2, ReadWrite = 3 }
```

### Rules

- Every enum used in a DTO, request type, or integration event lives in the `.Api` project.
- Enums serialise as strings. `JsonStringEnumConverter` is required in every service.
- `[Flags]` enums serialise as integers — the only permitted exception.
- Domain enums and Api enums have identical values (names and ordinals).
