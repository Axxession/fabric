# Error Handling

## Scope: domain failures only

`Result` is for **expected domain failures** — operations the aggregate knows can fail as part of normal business rules. It is not for infrastructure failures. A database that won't connect, a network timeout, a null reference — these are exceptions, and they should propagate as exceptions.

> **Rule**: If the caller cannot meaningfully handle the failure as a business decision, throw. If the aggregate is saying "this operation is not allowed in the current state", return `Result`.

## The `Result` types

```csharp
public readonly struct Result<TError, TValue>
{
    public bool IsSuccess(out TValue value) { ... }
    public bool IsFailure(out TError error) { ... }
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure) { ... }
}

public readonly struct Result<TError>
{
    public bool IsSuccess(out Unit value) { ... }
    public bool IsFailure(out TError error) { ... }
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<TError, TResult> onFailure) { ... }
}

public static class Result
{
    public static Result<TError> Success<TError>();
    public static Result<TError, TValue> Success<TError, TValue>(TValue value);
    public static Result<TError> Failure<TError>(TError error);
    public static Result<TError, TValue> Failure<TError, TValue>(TError error);
}
```

Always use the static `Result` class to construct them — never call the struct factories directly.

## Error type: a per-aggregate enum

```csharp
public enum KeyGroupError
{
    AlreadyLocked,
    CannotEditLocked,
}
```

One enum per aggregate. Name it `{Aggregate}Error` (singular). Each case names the failure in domain terms — no HTTP concepts, no message strings.

## Aggregate usage

```csharp
public Result<KeyGroupError> Lock()
{
    if (Locked)
        return Result.Failure<KeyGroupError>(KeyGroupError.AlreadyLocked);
    Locked = true;
    return Result.Success<KeyGroupError>();
}
```

For operations that return a value:

```csharp
public Result<KeyGroupError, KeySet[]> GetKeys()
{
    if (Locked)
        return Result.Failure<KeyGroupError, KeySet[]>(KeyGroupError.CannotEditLocked);
    return Result.Success<KeyGroupError, KeySet[]>(KeySets);
}
```

## Endpoint usage

```csharp
var result = keyGroup.Lock();

if (result.IsFailure(out var error))
{
    return error switch
    {
        KeyGroupError.AlreadyLocked    => Results.Problem("Key group is already locked.", statusCode: StatusCodes.Status409Conflict),
        KeyGroupError.CannotEditLocked => Results.Problem("Cannot edit a locked key group.", statusCode: StatusCodes.Status422UnprocessableEntity),
        _                              => Results.Problem("Unexpected error"),
    };
}

await session.SaveChangesAsync(ct);
return Results.NoContent();
```

The switch must be exhaustive. Always include a `_` default case.

### Extracting the error switch: `MapError`

```csharp
private static IResult MapError(ControllerError error) => error switch
{
    ControllerError.ReaderNotFound => Results.NotFound(),
    ControllerError.OutputNotFound => Results.NotFound(),
    _ => Results.Problem("Unexpected error.", statusCode: StatusCodes.Status500InternalServerError),
};
```

One `MapError` per endpoint class. The switch must remain exhaustive with a `_` default arm.

## Simple cases: inline returns

```csharp
if (keyGroup is null)
    return Results.NotFound();
```

No `Result` needed. Save `Result` for failures that originate inside the aggregate's business logic.

## Rules

- **`Result` is for domain failures only.** Infrastructure failures are exceptions — do not catch and wrap them in `Result`.
- **Always use the static `Result` class** — `Result.Success<TError>()`, `Result.Failure<TError>(error)`. Never call struct factories directly.
- **Never embed `HttpStatusCode` in a domain error.** The switch at the endpoint boundary is the only place where domain errors map to HTTP status codes.
- **One error enum per aggregate**, defined alongside it. Do not share across aggregates.
- **The switch must be exhaustive.** Always include a `_` default case.
- **Always use `Results.Problem(detail, statusCode: ...)`** for domain error responses. Never use `Results.Conflict(string)`, `Results.BadRequest(string)`, `Results.UnprocessableEntity(string)` — those overloads produce a plain-text body, not a `ProblemDetails` object.
