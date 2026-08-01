# API Versioning

## Strategy: URL Path Versioning

Use URL path versioning for all public APIs. The version is part of the route, making it explicit and cache-friendly.

```csharp
// Route configuration
var versionGroup = app.MapGroup("/v1");

versionGroup.MapGet("/key-groups", ListKeyGroups);
versionGroup.MapPost("/key-groups", CreateKeyGroup);
```

## When to Version

A new version is needed when an existing endpoint changes in a **breaking** way:

- Removing or renaming a field in the response
- Changing a required field to optional (or vice versa) in the request
- Changing the semantics of an existing field
- Removing an endpoint

Non-breaking changes (adding fields, adding new endpoints) do not require a new version.

## Deprecation Strategy

When a new version is released, the old version enters deprecation. Return a `Sunset` or `Deprecation` HTTP header on deprecated endpoints:

```csharp
return Results.Ok(response)
    .WithHeader("Sunset", "Sat, 31 Dec 2025 23:59:59 GMT");
```

Remove deprecated endpoints after the sunset date has passed and all known clients have migrated.

## Backward Compatibility

Maintain backward compatibility within a version. If you need to change behavior, either:
1. Add a new field and let the old field remain (deprecated).
2. Version the endpoint.

Do not change the meaning of an existing field within the same version.
