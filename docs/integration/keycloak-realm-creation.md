# Keycloak Realm Creation

## Purpose

Fabric can optionally use platform-level Keycloak credentials to provision a tenant realm.

This is separate from tenant-scoped Keycloak administration:

- realm creation is a platform operation
- user, role, and group management remains a tenant-scoped operation after provisioning

## Optional Platform Configuration

Realm provisioning is enabled only when appsettings contains `KeycloakRealmProvisioning`.

The platform config points Fabric to a Keycloak client in a bootstrap realm, typically `master`.

That bootstrap client must already exist in Keycloak and must be allowed to:

- create realms
- inspect clients in `master`
- read its own service-account user in `master`
- assign client roles in `master`

Fabric uses those `master` permissions to create the tenant realm, temporarily grant the bootstrap service account all roles exposed by the newly created `<tenant>-realm` admin client in `master`, refresh its token, then continue tenant-realm provisioning.

## Provisioning Trigger

Realm provisioning is an explicit platform action.

It is not automatic on tenant creation.

An operator creates or opens a tenant in Fabric, then triggers tenant Keycloak provisioning for that tenant.

The created Keycloak realm name matches the Fabric tenant id.

## What Provisioning Creates

For tenant `acme`, provisioning creates and configures these Keycloak resources.

### Realm

- realm name: `acme`
- realm is enabled

Immediately after realm creation, Fabric looks up admin client `acme-realm` in `master`, grants all roles on that client to the bootstrap service account, then requests a fresh access token before continuing realm initialization.

After provisioning completes or fails, Fabric removes those temporary `acme-realm` role grants from the bootstrap service account.

### Fabric Realm Roles

Fabric seeds tenant realm roles for tenant-side authorization use.

Provisioned roles:

- `admin`
- `host`
- `manager`
- `security-officer`
- `integrator`
- `contractor-enrollment`
- `contractor-planning`

`platform-admin` is not created in tenant realms.

### Portal Client

Fabric creates client `portal` for frontend sign-in.

Expected characteristics:

- OpenID Connect client
- authorization code flow enabled
- public client
- redirect URIs based on tenant base URL
- web origins based on tenant base URL

This client is intended for the Fabric frontend login flow.

### Realm Role Claim Mapping

Fabric configures the portal client so realm roles are exposed in the token under claim name `roles`.

This matches Fabric's authentication normalization behavior, which reads both `role` and `roles` claims.

### Fabric Admin Client

Fabric creates client `fabric` for tenant-scoped administrative integration.

Expected characteristics:

- OpenID Connect client
- confidential client
- service accounts enabled

Fabric reads the generated client secret and stores it in the tenant's Keycloak integration config.

### Initial Admin User

Fabric creates an initial realm user:

- username: `admin`
- first name: `Initial`
- last name: `Admin`
- email: `admin@<tenant>.local`

Fabric sets temporary password `axxession` and assigns all provisioned Fabric realm roles to this user.

The password must be changed on first sign-in.

### Service Account Roles

Fabric resolves the `fabric` client service-account user and grants all client roles from:

- `account`
- `realm-management`

This gives the tenant integration client the required management access inside the provisioned realm.

## What Fabric Updates After Provisioning

After Keycloak objects are created, Fabric links the tenant to the new realm by updating two tenant-owned configuration areas.

### Tenant OIDC Settings

Fabric sets tenant OIDC to the provisioned realm:

- `MetadataUrl` -> `https://<keycloak>/realms/<tenant>/.well-known/openid-configuration`
- `ClientId` -> `portal`

### Tenant Keycloak Integration

Fabric enables tenant Keycloak administration and stores:

- Keycloak base URL
- tenant realm name
- client id `fabric`
- generated client secret

After this step, tenant-scoped Fabric Keycloak administration can use the created `fabric` client.

## Operational Boundary

Realm provisioning is bootstrap.

It does not manage ongoing realm customization beyond the initial realm, clients, role mapper, and service-account role assignment.

If a later provisioning step fails after realm creation, Fabric may leave a partially initialized realm in Keycloak. Operators may need to clean up the failed realm before retrying.
