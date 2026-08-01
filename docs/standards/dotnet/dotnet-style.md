# C# Style and Modern Language Usage

Most rules are enforced by `.editorconfig`; the rest are hard rules enforced by convention and code review.

## Null checking

Always use `is null` / `is not null`. Never `== null` or `!= null`.

```csharp
// ✓
if (keyGroup is null) return Results.NotFound();
if (widget is not null) widget.Process();
```

`is null` uses pattern matching and is not overridable by a custom `==` operator.

## Pattern matching

Use `is` patterns instead of `as` + null check or explicit casts:

```csharp
// ✓
if (shape is Circle circle) { ... }
```

Use switch expressions over switch statements wherever the result is a value. Must be exhaustive — always include a `_` discard arm.

## Namespaces — file-scoped, always

```csharp
namespace SmartAccess.MyService.Endpoints;

public static class KeyGroupEndpoints { ... }
```

## `using` statements — do not scope unless the lifetime matters

Prefer `using` declarations over `using` blocks. Only use a block when the disposal boundary must be earlier than end-of-scope.

```csharp
using var stream = File.OpenRead(path);
```

## Nullability — enabled for all classes and records

```csharp
public class Widget
{
    public Guid Id { get; init; }
    public string Name { get; set; } = default!;   // required, initialised elsewhere
    public string? Description { get; set; }        // optional
}
```

Use `default!` to satisfy the compiler for framework-initialised properties. Do not use `#nullable disable`.

## `var` — use when the type is apparent

```csharp
// ✓ type obvious from RHS
var widget = new Widget();

// ✓ explicit type adds useful information
KeyGroup? keyGroup = await session.LoadAsync<KeyGroup>(id, ct);
```

Explicit type is important for nullable returns (`T?`) and interface types.

## Collection expressions

Prefer collection expressions (C# 12) over `new List<T>()` and array initializers:

```csharp
int[] ids = [1, 2, 3];
List<string> names = ["Alice", "Bob"];
string[] empty = [];
int[] combined = [..first, ..second];
```

## Primary constructors

Use for classes whose constructor does nothing except assign fields (C# 12):

```csharp
public class TemplateService(IDocumentSession session, ILogger<TemplateService> logger)
{
    public async Task<Template?> GetAsync(Guid id, CancellationToken ct) =>
        await session.LoadAsync<Template>(id, ct);
}
```

Do not use when the constructor has meaningful logic.

## Target-typed `new`

```csharp
Widget widget = new() { Name = "Foo" };
```

Do not use as a substitute for `var` when the type would otherwise be unclear.

## `required` properties

Use over constructor enforcement for DTOs and plain entities:

```csharp
public class CreateWidgetRequest
{
    public required string Name { get; init; }
    public required Guid TenantId { get; init; }
}
```

## `record` types

Use for value objects and immutable DTOs. Use `class` for entities and aggregates.

```csharp
public record TemplateSpecification(string Format, int Version);
```

## Naming quick reference

| Symbol | Convention | Example |
|---|---|---|
| Class, record, struct, interface | `PascalCase` | `KeyGroupEndpoints` |
| Method, property, event | `PascalCase` | `GetKeyGroup`, `IsLocked` |
| Private field | `_camelCase` | `_session` |
| Parameter, local variable | `camelCase` | `keyGroupId` |
| Constant | `PascalCase` | `DefaultPageSize` |
| Enum type | `PascalCase` singular | `KeyGroupError` |
| Enum member | `PascalCase` | `AlreadyLocked` |
| Generic type parameter | `T` prefix | `TError`, `TValue` |

## Logging

### Source generation: `[LoggerMessage]` required

All log statements use `[LoggerMessage]` source generation. Raw `_logger.LogInformation(...)` with string interpolation is not permitted.

```csharp
internal static partial class KeyGroupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "KeyGroup {KeyGroupId} locked")]
    public static partial void KeyGroupLocked(this ILogger logger, Guid keyGroupId);
}
```

### What and when to log

| Level | When to use |
|---|---|
| Trace | Fine-grained diagnostic detail. Development only. |
| Debug | Request/response shapes, resolved values, branch decisions. |
| Information | Significant domain events — resource created, state transition, job finished. |
| Warning | Expected failure conditions — not found, precondition failed, retryable errors. |
| Error | Unexpected failures — exceptions. Always include the exception object. |
| Critical | System-level failures requiring immediate attention. |

**Rules:**
- No request/response shape logging at `Information` or above. Shapes belong at `Debug`/`Trace`.
- One `Information` log per meaningful operation — not one per line of handler code.
- Always pass the `Exception` object to `LogError`/`LogCritical`.
- No string interpolation in log messages — use structured logging parameters (`{KeyGroupId}`).
