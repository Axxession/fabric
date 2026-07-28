# Architecture Debt

## Cross-DbContext Atomicity

### Status

Unresolved.

### Context

Fabric is intentionally designed as separate bounded contexts/domains so parts can later be extracted independently. That split is valuable, but it makes one business operation spanning multiple `DbContext` instances hard to keep atomic.

Sharing one physical database does not remove this problem. Each EF Core `DbContext` has its own connection lifecycle unless explicitly coordinated. That coordination becomes even less realistic if these domains are later pulled apart into separate services.

### Current Failure

The employee create/update flow attempted to share a transaction between `EmployeesDbContext` and `IdentitiesDbContext`.

Current issue paths:

- `src/backend/Fabric.Server/Employees/Application/EmployeeService.cs:74-82`
- `src/backend/Fabric.Server/Employees/Application/EmployeeService.cs:141-145`

Affected flows:

- `CreateEmployeeAsync`
- `UpdateEmployeeAsync`

Observed runtime failure:

```text
System.InvalidOperationException: The specified transaction is not associated with the current connection. Only transactions associated with the current connection may be used.
```

Root cause:

- `EmployeesDbContext` started the transaction.
- `IdentitiesDbContext` attempted `UseTransactionAsync(transaction.GetDbTransaction(), ...)`.
- Both contexts were registered separately with `AddDbContext(...)` and did not share the same connection instance.

### Temporary Decision

The broken cross-`DbContext` transaction usage was removed from `EmployeeService`.

Temporary save order:

- Save `EmployeesDbContext` first.
- Save `IdentitiesDbContext` second.
- Only run follow-up lifecycle/automation side effects after both saves succeed.

This is intentionally not treated as a final solution. It avoids the current runtime exception while we keep the bounded-context split intact and evaluate a better consistency model.

### Known Risks After Transaction Removal

- Employee row can exist without matching identity row if identity save fails.
- Employee update can succeed while identity profile update fails.
- Retries must later be idempotent to avoid duplicate affiliations or duplicate sync work.
- Reads across domains may observe partial state temporarily.
- Manual recovery or background reconciliation may be required after partial writes.

### Why We Are Not Collapsing Contexts

The current architecture is built around separate domains that should remain easy to pull out later. Solving this by collapsing persistence boundaries would reduce the immediate problem, but it would also weaken that architectural direction.

### Repo Transaction Audit

Broken cross-`DbContext` transaction usage:

- `src/backend/Fabric.Server/Employees/Application/EmployeeService.cs`
  - `CreateEmployeeAsync`
  - `UpdateEmployeeAsync`

Local single-`DbContext` transaction usage kept in place:

- `src/backend/Fabric.Server/Employees/Application/EmployeeService.cs`
  - `MoveOrganizationUnitAsync`
  - Reason: one `EmployeesDbContext`, closure-row rebuild should stay atomic.

- `src/backend/Fabric.Server/Desfire/Application/DesfireDeviceLeaseStore.cs`
  - Reason: advisory lock + lease creation uses one `DesfireDbContext`.

- `src/backend/Fabric.Server/Desfire/Application/DesfireVariableResolver.cs`
  - Reason: advisory lock + sequence increment uses one `DesfireDbContext`.

No current repo usage found for:

- `TransactionScope`
- `CurrentTransaction`

### Broader Cross-Boundary Consistency Debt

The current exception only hit `EmployeeService`, but there are other high-coupling services where one business operation depends on multiple bounded contexts and can still suffer partial-apply or immediate-consistency debt:

- `src/backend/Fabric.Server/Sagas/EmployeeLifecycle/EmployeeLifecycleAutomationService.cs`
  - Spans `Sagas`, `Employees`, `Identities`, `AccessCatalog`, and `AccessControl`.

- `src/backend/Fabric.Server/AccessCatalog/Application/AccessGrantService.cs`
  - Grant persistence and provisioning workflow enqueue are separate boundaries.

- `src/backend/Fabric.Server/AccessCatalog/Application/PackageRequestService.cs`
  - Uses live data from `AccessCatalog`, `Employees`, `Identities`, and `Locations` in one business flow.

These are not the same bug, but they are part of the same architecture debt theme: domain separation vs cross-boundary consistency.

### Long-Term Options

- Shared connection/unit-of-work while domains still share one database.
- Outbox in source domain plus worker-based synchronization.
- Saga/process manager with explicit compensating actions.
- Explicit async synchronization model with visible sync state.

### Likely Direction

The architecture currently leans toward eventual consistency/outbox-style patterns more than cross-context atomic writes, because the domain split is a deliberate design choice and future service extraction remains a goal.
