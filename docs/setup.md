# Backend Setup

Fabric backend config lives in `src/backend/Fabric.Server/appsettings.json` and environment-specific overrides such as `appsettings.Development.json`.

Secrets should not be committed. Use environment-specific config, user secrets, environment variables, or a secret store for production values.

## Minimal Configuration

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Username=user;Password=password;Database=fabric"
  },
  "Cors": {
    "Origins": ["http://localhost:5173"]
  },
  "Tenancy": {
    "Mode": "SingleTenant",
    "TenantBaseUrl": "http://localhost:5173",
    "DefaultTenant": {
      "Id": "main-tenant",
      "Oidc": {
        "MetadataUrl": "http://localhost:7080/realms/dev/.well-known/openid-configuration",
        "ClientId": "portal",
        "RequireHttpsMetadata": false
      }
    }
  },
  "Storage": {
    "Provider": "FileSystem"
  },
  "AllowedHosts": "*",
  "EnableSwagger": true
}
```

## File Storage

Fabric uses `ManagedCode.Storage` as the default storage abstraction for uploaded files.

Config lives under `Storage`.

If `Storage` is missing or `Storage:Provider` is omitted, Fabric falls back to local file system storage.

### Default File System Storage

Default file system root path:

- Linux: `~/.local/share/fabric/storage`
- Other platforms: the current user's local application data folder plus `fabric/storage`

Override it with `Storage:FileSystem:BasePath`.

```json
{
  "Storage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "BasePath": "/var/lib/fabric/storage"
    }
  }
}
```

### Azure Blob Storage

```json
{
  "Storage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "Container": "fabric"
    }
  }
}
```

Required Azure fields:

- `ConnectionString`
- `Container`

`PublicAccessType` is optional and defaults to `None`.

### Storage Path Convention

Tenant-scoped stored files use this path layout inside the configured storage root or blob container:

`/{tenant}/{domain}/{owner-scope}/{fileId}`

Example for kiosk assets:

`/main-tenant/kiosk/profiles/{profileId}/assets/{fileId}`

This keeps tenant deletion simple because a whole tenant can be removed by deleting the `{tenant}` prefix.

### Stored File Metadata

Uploaded files should persist app metadata in the domain database table that owns them. At minimum store:

- storage path
- visibility (`Public` or `Private`)
- file name
- content type
- size
- uploader snapshot (`oid`, `email`, `displayName`)

Public files should still be exposed through Fabric-owned endpoints by default. Private files should only be served through authenticated or policy-protected endpoints.

## Tenancy Modes

`Tenancy:Mode` controls how tenant config is resolved.

`Tenancy:TenantBaseUrl` controls externally visible platform URLs used outside request handling, such as visitor email links. It may contain a `{tenant}` placeholder. In `SingleTenant` mode the placeholder can be omitted.

Allowed values:

- `SingleTenant`
- `MultiTenant`

### SingleTenant

Use `SingleTenant` for local dev, demos, or deployments where one backend serves one tenant.

The backend uses `Tenancy:DefaultTenant` to seed the default tenant when migrations run.

```json
{
  "Tenancy": {
    "Mode": "SingleTenant",
    "TenantBaseUrl": "https://portal.example.com",
    "DefaultTenant": {
      "Id": "main-tenant",
      "Oidc": {
        "MetadataUrl": "https://login.example.com/.well-known/openid-configuration",
        "ClientId": "fabric-portal",
        "RequireHttpsMetadata": true
      }
    }
  }
}
```

Required `DefaultTenant` fields in `SingleTenant` mode:

- `Id`: Fabric tenant id.
- `Oidc:MetadataUrl`: OIDC discovery document URL.
- `Oidc:ClientId`: Portal client id.
- `Oidc:RequireHttpsMetadata`: Set `false` only for local HTTP identity providers.

### MultiTenant

Use `MultiTenant` when one backend serves multiple tenants.

```json
{
  "Tenancy": {
    "Mode": "MultiTenant",
    "TenantBaseUrl": "https://{tenant}.example.com"
  },
  "AdminOidc": {
    "MetadataUrl": "https://login.example.com/.well-known/openid-configuration",
    "ClientId": "fabric-admin",
    "RequireHttpsMetadata": true
  }
}
```

In `MultiTenant` mode, tenant-specific settings are stored in the tenancy database and loaded at runtime. `AdminOidc` config is required for admin authentication.

Required `AdminOidc` fields in `MultiTenant` mode:

- `MetadataUrl`: OIDC discovery document URL.
- `ClientId`: Admin client id.
- `RequireHttpsMetadata`: Set `false` only for local HTTP identity providers.

## Keycloak Realm Provisioning

Platform Keycloak realm provisioning is optional.

If `KeycloakRealmProvisioning` is configured, platform administrators can provision a tenant Keycloak realm from Fabric. Fabric uses this config to authenticate against a bootstrap realm such as `master`.

```json
{
  "KeycloakRealmProvisioning": {
    "Url": "https://login.example.com",
    "Realm": "master",
    "ClientId": "fabric-platform",
    "ClientSecret": "replace-with-secret"
  }
}
```

Required fields when this section is present:

- `Url`: Keycloak base URL.
- `Realm`: Bootstrap realm, often `master`.
- `ClientId`: Bootstrap client id.
- `ClientSecret`: Bootstrap client secret.

Provisioning creates a tenant realm named after the Fabric tenant id, creates client `portal` for frontend login, creates client `fabric` for tenant administration, grants the `fabric` service account all `account` and `realm-management` client roles, then links the created realm back to the tenant's OIDC and Keycloak integration settings.

Provisioning also seeds tenant realm roles for Fabric authorization, excluding `platform-admin`, and creates an initial `admin` user with temporary password `axxession` and all seeded tenant roles.

See `docs/integration/keycloak-realm-creation.md` for the provisioning model and boundary.

## Email Configuration

Email is optional. If no email config exists, email send attempts return `NotConfigured`.

Platform email config is set through appsettings under `Email:Graph`. It acts as the default mail configuration for the platform.

```json
{
  "Email": {
    "Graph": {
      "FromEmail": "noreply@example.com",
      "FromName": "Fabric",
      "AzureTenantId": "00000000-0000-0000-0000-000000000000",
      "ApplicationId": "00000000-0000-0000-0000-000000000000",
      "Secret": "replace-with-secret",
      "SaveSentItems": false
    }
  }
}
```

`Email:Graph` is all-or-nothing. If `Graph` is present, these fields are required:

- `FromEmail`
- `FromName`
- `AzureTenantId`
- `ApplicationId`
- `Secret`

`SaveSentItems` is optional and defaults to `false`.

### Tenant Email Override

Tenants can override platform email config at runtime through the portal.

Resolution order:

1. Tenant-specific Graph email config.
2. Platform `Email:Graph` config from appsettings.
3. `NotConfigured` if neither exists.

Tenant overrides are also all-or-nothing. A tenant either uses a complete Graph email config or falls back to the platform default. Individual tenant fields do not fall back to individual platform fields.

In `SingleTenant` mode, the initial seeded tenant can also receive an email override through `Tenancy:DefaultTenant:GraphEmail`.

```json
{
  "Tenancy": {
    "Mode": "SingleTenant",
    "DefaultTenant": {
      "Id": "main-tenant",
      "Oidc": {
        "MetadataUrl": "https://login.example.com/.well-known/openid-configuration",
        "ClientId": "fabric-portal",
        "RequireHttpsMetadata": true
      },
      "GraphEmail": {
        "FromEmail": "tenant@example.com",
        "FromName": "Tenant Name",
        "AzureTenantId": "00000000-0000-0000-0000-000000000000",
        "ApplicationId": "00000000-0000-0000-0000-000000000000",
        "Secret": "replace-with-secret",
        "SaveSentItems": false
      }
    }
  }
}
```

## Environment Variables

ASP.NET Core configuration supports environment variable overrides with `__` as a section separator.

Examples:

```bash
ConnectionStrings__Database="Host=localhost;Username=user;Password=password;Database=fabric"
Tenancy__Mode="SingleTenant"
Tenancy__TenantBaseUrl="https://portal.example.com"
Tenancy__DefaultTenant__Id="main-tenant"
Tenancy__DefaultTenant__Oidc__MetadataUrl="https://login.example.com/.well-known/openid-configuration"
Tenancy__DefaultTenant__Oidc__ClientId="fabric-portal"
Email__Graph__FromEmail="noreply@example.com"
Email__Graph__FromName="Fabric"
Email__Graph__AzureTenantId="00000000-0000-0000-0000-000000000000"
Email__Graph__ApplicationId="00000000-0000-0000-0000-000000000000"
Email__Graph__Secret="replace-with-secret"
Storage__Provider="FileSystem"
Storage__FileSystem__BasePath="/var/lib/fabric/storage"
Storage__Azure__ConnectionString="UseDevelopmentStorage=true"
Storage__Azure__Container="fabric"
```
