# AGENTS.MD

## Repo Shape
- Frontend: `src/frontend` React + Vite + TS, TanStack Router/Query, Tailwind v4 tokens in `src/styles.css`, shadcn-style local UI primitives in `src/shared/components/ui`, OpenAPI client in `src/shared/api`.
- Backend: `src/backend/Fabric.Server` ASP.NET Core `net10.0`, controllers, EF Core 10 + Npgsql, Swagger/OpenAPI, Mapperly, hosted migration runner.
- Backend modules: `AccessPolicies`, `Locations`, `Reception`, `Visitors`, `Sagas`; common code in `Core`, infra in `Infrastructure`.
- Module shape: `Domain`, `Contracts`, `Endpoints`, `Persistence`, `Application` where needed.

## Commands
- Frontend dev/build/test: `cd src/frontend && npm run dev|build|test`.
- Generate frontend API types: backend must run on `http://localhost:5245`, then `cd src/frontend && npm run api:generate`.
- Backend build: `cd src/backend && dotnet build Fabric.Server/Fabric.Server.csproj /bl:{}`.
- Backend Swagger source: `http://localhost:5245/swagger/v1/swagger.json`.
- After backend contract/endpoint changes: build backend, run backend, run `npm run api:generate`, then `npm run build`.

## Backend Conventions
- Nullable enabled, implicit usings, warnings as errors.
- JSON enums as strings, JSON property names camelCase, Swagger params camelCase.
- Controllers in `*/Endpoints`; one operation per method; use `[FromBody]` for POST/PUT bodies.
- Contracts in `*/Contracts`; use `{Verb}{Noun}Request`, `{Noun}Response`.
- Domain owns behavior; add methods like `UpdateProfile`, `Deactivate`, etc. Do not bypass domain methods when behavior exists.
- EF configs in `*/Persistence/Configuration`; DbContexts per module.
- Lists use `BaseListRequest`, `IPaged<T>`, zero-based pages.
- Return 404 for missing resource, 409 for conflicts, 204 for successful delete/deactivate with no body.

## Frontend API
- Do not hand-copy backend DTOs. Use generated OpenAPI types.
- Typed client: `src/shared/api/client.ts`.
- Generated schema: `src/shared/api/generated/schema.d.ts`; do not edit by hand.
- Query keys include page/filter params. Invalidate broad entity keys after create/update/delete.

## Frontend Layout
- Pages start left; do not center top-level page content.
- Use cards/panels with `rounded-structural border border-border bg-content`.
- List page card header: title + subtitle, optional primary action on right.
- Detail/create/edit page: separate header above card with icon-only history back button + title + subtitle. Form card can be headerless.
- Back button: icon-only `ArrowLeft`, `aria-label="Go back"`, `window.history.back()`.

## Frontend CRUD UX
- Create success: `toast.success(...)` + invalidate queries + `window.history.back()`.
- Edit success: `toast.success(...)` + invalidate queries; stay on page unless requested otherwise.
- Errors: prefer inline error banner near form/table.
- Delete: icon-only trash button + `window.confirm(...)` before mutation.
- Add action: primary button with `Plus` icon + text.
- Edit action: icon-only `Pencil` with accessible label.
- Delete action: icon-only `Trash2` with accessible label.

## Tables, Empty, Paging
- Use table for non-empty result sets.
- If no items, show shadcn-style `Empty`; do not render empty table.
- If entries exist, always show pager, even one page, so total count is visible.
- Pager shows total text like `Showing x-y of n ...`.
- No pager for empty state.
- Page state is zero-based to match backend; labels are one-based.

## Toasts
- Global toaster mounted in `AppProviders`.
- Use `sonner` via `toast.success(...)`.
- Keep toaster global so toast survives create-page back navigation.

## Design Tokens
- Colors: `background #f8f8f8`, `content #ffffff`, `primary #238cff`, `foreground #212529`, `muted #6c757d`, `border #dddddd`, `hoverBlue #eef6ff`, `activeBlue #deeeff`, `hoverGray #f3f3f3`, `success #00c950/#e6faeb`, `error #ff6467/#feeaea`.
- Radius: status `5px`; interactive/buttons/inputs/clickable cards `8px`; panels/cards/containers/dialogs `12px`; fixed/floating actions `20px`.
- Typography: implementation uses `Satoshi`, fallback `DM Sans`, Arial. H2 `20/600`, H3 `16/500`, body `14/400`, body strong `14/600`, small `12/400`.
- Buttons: primary blue bg + white text + `rounded-interactive`; hover via color/opacity only. Small action buttons: icon-only `size-9`.
- Icons: use `lucide-react`; keep meanings consistent: add `Plus`, edit `Pencil`, delete `Trash2`, back `ArrowLeft`.
- Effects: no shadows, no text/drop shadows. Hover via color changes only. Prefer subtle borders/spacing over decoration.

## Editing Rules
- Keep changes small and aligned with existing modules.
- Prefer shared UI primitives under `src/shared/components/ui` for repeated shadcn-style components.
- Do always generate backend API types; do not hand-copy DTOs. (Requires running the backend)
- Preserve generated files; regenerate with `npm run api:generate` only.
- Run relevant build/tests before final response.
