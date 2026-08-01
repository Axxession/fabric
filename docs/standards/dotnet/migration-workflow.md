# EF Core Migration Workflow

## Adding a Migration

Use descriptive names that summarize the change:

```bash
dotnet ef migrations add AddKeyGroupLockedField
dotnet ef migrations add CreateCompanyTable
dotnet ef migrations add RenameKeyGroupOwnerToTenant
```

**Naming convention:** `{Verb}{Entity}{Detail}` — imperative, descriptive, PascalCase.

Run from the server project directory containing the `DbContext`.

## Reviewing Migrations

Always review the generated migration code before applying. Check that:
- `Up()` and `Down()` are symmetric.
- No accidental shadow properties were created.
- Column names match the configured table/column naming.
- Indexes are intentional (EF Core creates indexes for foreign keys by default — suppress with `.HasIndex(...).IsUnique(false)` if not needed).

## Applying Migrations

| Environment | Method |
|---|---|
| Local development | `dotnet ef database update` |
| CI/CD | `dotnet ef migrations script --output migrate.sql` — apply the script as part of deployment |
| Production | SQL script reviewed and applied by DBA — never automatic |

**Rule:** Never use `EnsureCreated()` or `Database.Migrate()` in production code. Use generated SQL scripts that are reviewed and version-controlled.

## Squashing Migrations

When a feature branch accumulates many small migrations, squash them into a single migration before merging:

1. Remove existing migration files for the feature.
2. Add a new migration with a descriptive name covering the full change set.
3. Verify the generated SQL matches the expected schema.

**When to squash:**
- Before merging a feature branch with 5+ migrations.
- Before a major release to reset the migration history.
- Never squash migrations that have already been applied to production — create a new baseline migration instead.

## Migration History

The `__EFMigrationsHistory` table tracks which migrations have been applied. Never modify this table manually. If a migration needs to be rolled back in development, use `dotnet ef migrations remove` (if not yet applied) or revert with a new migration (if already applied).
