# Actor Resolution

Authentication and current-user resolution are separate concerns.

`ClaimsPrincipal` remains infrastructure-level authentication state. It should only expose raw token claims such as:

- directory id from `oid`, fallback `sub`
- email as fallback identifier
- OIDC authorization roles such as `Admin` and `SecurityOfficer`

Application and frontend-facing user context should be resolved through a dedicated `CurrentActor` read model.

Recommended shape:

```text
CurrentActor
- IdentityId?
- EmployeeId?
- OrganizationUnitId?
- ManagerEmployeeId?
- DisplayName?
- FirstName?
- LastName?
- Email?
- DirectoryId?
- IsEmployee
- IsManager
- IsAdmin
- IsSecurityOfficer
- Roles[]
```

Resolution rules:

- Match employee by `DirectoryId` using JWT claim `oid`, fallback `sub`.
- Fallback to employee `Email` when no directory id match exists.
- `IsEmployee` is true when an employee match exists.
- `IsManager` is derived from employee hierarchy: an employee with direct reports is a manager.
- `IsAdmin` and `IsSecurityOfficer` come from OIDC role claims, not from employee facts.

Recommended module placement:

- a dedicated `Actors` application/read-model module
- not a separate bounded context with its own database
- not inside `Infrastructure`, because it combines claims, employee facts, identity facts, and frontend-facing denormalized output

Caching strategy:

- request-scoped memoization first
- shared `IMemoryCache` second
- current default policy: 30 minute absolute expiration and 10 minute sliding expiration
- cache entries should be addressable by:
  - `IdentityId`
  - `DirectoryId`
  - `Email`

Invalidation hooks should exist from the start, even if not yet wired everywhere:

- `InvalidateByIdentityId`
- `InvalidateByDirectoryId`
- `InvalidateByEmail`

Frontend should get denormalized current-user information through a dedicated endpoint such as:

```text
GET /api/actors/me
```

This endpoint is the source for deciding what the UI should show.

Open authorization question:

- Is `SecurityOfficer` global per tenant, or scoped to one or more locations?

If `SecurityOfficer` is location-scoped, it should not remain only an OIDC role claim. It would need domain data similar to approval-group scoping or a dedicated location-scoped authorization model. This remains unresolved.
