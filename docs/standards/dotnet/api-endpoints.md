# Endpoints, Validation, and Pagination

## Endpoint Naming Conventions

**One class per entity. One file per entity. One method per operation.**

All operations on a given entity live in a single file:

```
Endpoints/KeyGroupEndpoints.cs  — Create, Get, List, Update, Delete, Lock
```

| Method name | HTTP | When | Example |
|---|---|---|---|
| `Get{Noun}` | GET | Single resource by ID | `GetKeyGroup` |
| `List{Noun}s` | GET | Collection or paginated list | `ListKeyGroups` |
| `Create{Noun}` | POST | Create a new resource | `CreateKeyGroup` |
| `Update{Noun}` | PUT | Full replacement | `UpdateKeyGroup` |
| `Patch{Noun}` | PATCH | Partial update | `PatchKeyGroup` |
| `Delete{Noun}` | DELETE | Remove a resource | `DeleteKeyGroup` |
| `{Verb}{Noun}` | POST | Domain lifecycle action | `LockKeyGroup` |

**Rules:**
- File name: `{Noun}Endpoints.cs` — plural, entity-level
- Class name matches file name exactly
- Use domain verb for lifecycle operations — not `UpdateKeyGroupLocked`
- Plural noun for list methods, singular for everything else

### Parameter Binding Attributes

Always use explicit binding attributes:

| Source | Attribute | Example |
|---|---|---|
| Route segment | *(inferred)* | `Guid id` |
| Query string — scalar | `[FromQuery]` | `[FromQuery] string? filter` |
| Query string — object | `[AsParameters]` | `[AsParameters] ListWidgetsRequest request` |
| Request body | `[FromBody]` | `[FromBody] CreateWidgetRequest request` |
| DI service | *(inferred)* | `SitesDbContext db` |

**Rule**: POST/PUT handlers must annotate the request body with `[FromBody]`. List handlers must use `[AsParameters]` on the request object.

## Validation

Use **FluentValidation** for all inbound `Request` type validation. Validators are discovered automatically by convention.

### Where validators live

All validators in the server project under `Validators/`, organised into module sub-folders:

```
server/Validators/
├── Identity/CreatePersonRequestValidator.cs
├── Sites/CreateControllerRequestValidator.cs
└── Policies/CreateAccessRuleRequestValidator.cs
```

**Rule**: Never inject `IValidator<T>` directly into handlers. Validators are registered globally via `AddValidatorsFromAssemblyContaining<Program>()` and invoked automatically by validation middleware.

### Shape and required-field validation

```csharp
public sealed class ScheduleVisitRequestValidator : AbstractValidator<ScheduleVisitRequest>
{
    public ScheduleVisitRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End)
            .NotEmpty()
            .GreaterThan(x => x.Start).WithMessage("End must be after Start");
    }
}
```

### Uniqueness and server-side validation

Inject `DbContext` directly:

```csharp
public sealed class CreateKeyGroupRequestValidator : AbstractValidator<CreateKeyGroupRequest>
{
    public CreateKeyGroupRequestValidator(ServiceDbContext db)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MustAsync(async (name, ct) =>
                !await db.KeyGroups.AnyAsync(k => k.Name == name, ct))
            .WithMessage("A key group with this name already exists");
    }
}
```

### Validation failures vs. domain failures

| Failure type | Caught by | HTTP response |
|---|---|---|
| Missing or malformed input | FluentValidation, before handler | 400 Bad Request |
| Domain rule violation | Aggregate method, `Result.Failure(...)` | mapped via `ToProblemDetails()` |
| Not found | Inline check in handler | 404 Not Found |

For paginated list endpoint conventions, see `dotnet/pagination.md`.
