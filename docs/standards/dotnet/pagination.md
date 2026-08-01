# Pagination

All list endpoints return `IPaged<T>`. Pages are **zero-based** (`Page = 0` is first). Default page size is `25`.

## Request types

```csharp
public abstract class BaseListRequest : Pageable
{
    public string? SortColumn { get; set; }
    public bool? SortAscending { get; set; }
}

public class ListWidgetsRequest : BaseListRequest
{
    public string? Name { get; set; }
}
```

Bind with `[FromQuery]`.

## IDs filter

Every list endpoint on an entity with a `Guid` PK **must** expose an `Ids` filter as a separate `[FromQuery]` parameter on the handler method:

```csharp
private static async Task<IResult> ListWidgets(
    [AsParameters] ListWidgetsRequest request,
    [FromQuery] Guid[]? ids,
    AppDbContext db,
    CancellationToken ct)
{
    var query = db.Widgets.AsQueryable();
    if (ids is { Length: > 0 })
        query = query.Where(w => ids.Contains(w.Id));
    ...
}
```

`Guid[]` binds from repeated query-string values: `?ids=guid1&ids=guid2`. Do **not** use `List<Guid>` or put `Ids` inside `[AsParameters]`.

## Hard rules

- Pages are **zero-based** — `Page = 0` is first.
- List request types **always extend `BaseListRequest`** — not `Pageable` directly.
- Every list endpoint on a `Guid` PK entity **must** include `[FromQuery] Guid[]? ids`.
- All query-string parameters on list requests must be optional.
- **`IPaged<T>` is the return contract** — do not unwrap to a plain list.
- **`[ProducesResponseType<IPaged<T>>(...)]`** required on every list endpoint.
