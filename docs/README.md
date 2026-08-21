
When a task matches one of the conditions below, use the Read tool to load the corresponding file. Treat loaded content as mandatory instructions.

### .NET / C# Coding Standards

| File | Covers | Load when... |
|---|---|---|
| `standards/dotnet/domain-modeling.md` | Entities, Value Objects, Aggregates, Decision Flowchart | designing domain models |
| `standards/dotnet/api-contracts.md` | DTO naming, Mapperly, Polymorphic types, Enums | defining request/response types |
| `standards/dotnet/api-endpoints.md` | Endpoint naming, Validation, Parameter binding | writing handlers |
| `standards/dotnet/pagination.md` | List endpoints, IPaged, IDs filter, zero-based pages | implementing list endpoints |
| `standards/dotnet/error-handling.md` | Result types, Error enums, endpoint mapping | implementing error handling |
| `standards/dotnet/ef-core-config.md` | Entity configurations, OwnsMany, value object mapping | configuring EF Core |
| `standards/dotnet/dotnet-style.md` | C# style, null checking, naming conventions | writing any C# code |
| `standards/dotnet/testing.md` | Unit testing aggregates, integration tests, mocking strategy | writing tests |
| `standards/dotnet/di-conventions.md` | Service lifetimes, registration patterns, factories | wiring up DI |
| `standards/dotnet/middleware-pipeline.md` | Middleware order, exception handling, CORS | configuring the HTTP pipeline |
| `standards/dotnet/configuration.md` | Options pattern, appsettings, env overrides, startup validation | managing configuration |
| `standards/dotnet/background-jobs.md` | BackgroundService, graceful shutdown, logging | implementing background jobs |
| `standards/dotnet/api-versioning.md` | URL path versioning, deprecation, backward compat | versioning the API |
| `standards/dotnet/migration-workflow.md` | EF Core migration naming, squashing, applying | managing database migrations |

### Design

| File | Covers | Load when... |
|---|---|---|
| `standards/ui.md` | Theme tokens, shell layout, typography, tabs, lists, badges, actions | designing or changing frontend pages, components, layouts, or visual patterns |
| `design/access-domain-overview.md` | Access-domain glossary, ownership split, dependency direction | choosing bounded-context ownership or high-level access-domain structure |
| `design/contractors.md` | Contractor companies, contractors, job types, jobs, assignments, identity linkage boundary | designing or changing contractor planning, contractor jobs, contractor assignments, or contractor identity linkage |
| `design/locations.md` | Location hierarchy, location ownership, boundary rules | designing or changing location models and location references |
| `design/access-control.md` | PACS systems, access items, PACS targets, provisioning, subject import | changing PACS integrations, access-level mappings, provisioning, or PACS onboarding |
| `design/access-catalog.md` | Catalogs, packages, requests, approvals, grants | changing package composition, request flows, approvals, or grants |
| `design/employees.md` | Employee facts, personas, work locations, lifecycle calculation | changing employee sync, hierarchy, personas, or lifecycle facts |
| `design/actors.md` | Current actor resolution, caching, `/api/actors/me` shape | changing current-user resolution or frontend actor context |
| `design/automation-sagas.md` | Automatic grants, visitor automation, lifecycle side effects | changing automation rules, sagas, or lifecycle-driven access behavior |
| `design/credential-management.md` | Credential issuance, ranges, recycle rules, PACS credential targeting | changing credential allocation, issuance, recycle, or PACS credential provisioning |
| `design/printing.md` | PrintDesign ownership, template parsing, rendering split, DESFire/label integration boundaries | changing visual print designs, rendering, card/label template storage, or print-job ownership |
| `design/learning.md` | Learning bounded-context ownership, SCORM course delivery, enrollments, attempts, and requirement integration seam | designing or changing learning, LMS, SCORM course delivery, enrollments, attempts, or course-to-requirement integration |
| `design/reception-kiosk.md` | Reception kiosk onboarding sessions, step ownership, storage, terminal flow, and future check-in split | designing or changing the reception kiosk onboarding or session-driven kiosk flow |
| `design/access-cross-context-use-cases.md` | End-to-end examples across access domains | validating or understanding cross-context access flows |

### Integrations

| File | Covers | Load when... |
|---|---|---|
| `integration/keycloak-tenant.md` | Tenant-scoped Keycloak user management, groups, roles, membership, password reset, and boundary to realm provisioning | changing or understanding the tenant Keycloak integration |
| `integration/keycloak-realm-creation.md` | Platform-scoped Keycloak realm provisioning for tenants, master-realm credentials, created clients, role mapping, and tenant linking flow | changing or understanding tenant realm bootstrap and Keycloak realm creation |
