# Tenant Keycloak Integration

## Purpose

Fabric can integrate a tenant with Keycloak for identity administration. This integration lets Fabric manage tenant access structures in Keycloak while Keycloak remains the identity and authentication system of record.

This document describes the tenant-scoped domain concepts of the integration. It does not describe low-level API behavior.

Realm bootstrap and realm provisioning are separate concerns and should be documented separately.

## Tenant Keycloak Integration

A tenant Keycloak integration means one Fabric tenant is connected to one Keycloak realm for administrative operations.

Fabric stores tenant-specific Keycloak connection details and uses them to perform Keycloak management tasks for authorized Fabric operators.

The integration is tenant-scoped:

- each Fabric tenant can enable or disable Keycloak integration independently
- user management actions apply only to the connected Keycloak realm for that tenant
- if the integration is disabled, Fabric does not manage users, roles, or groups in Keycloak for that tenant

## What Fabric Manages In Keycloak

Fabric currently treats Keycloak as the managed identity directory for a tenant in these areas:

- users
- roles
- groups
- user password resets
- user-to-group membership
- user-to-role assignment
- group-to-role assignment

These concepts are used in Fabric as operational access-management structures.

### Users

A Keycloak user is a login-capable identity in the tenant realm.

Fabric can manage core administrative properties for a user:

- username
- first name
- last name
- email
- active or inactive state

Fabric can also reset a user password and decide whether the password is temporary.

A temporary password means Keycloak should require the user to change the password on next sign-in.

### Roles

A Keycloak role represents an authorization capability in the tenant realm.

Fabric uses roles as assignable access units that can be attached to:

- individual users
- groups

Roles are managed as tenant-owned authorization building blocks inside the connected realm.

### Groups

A Keycloak group represents a reusable membership container.

Fabric uses groups to organize users and apply shared role assignments.

A group can:

- contain users
- have roles assigned to it

This allows access to be granted to many users through membership instead of repeated individual assignment.

## Membership And Assignment Concepts

Fabric treats user membership and role assignment as separate administrative concerns.

Examples:

- a user can join or leave a group without changing the group definition
- a role can be assigned directly to a user for exceptional access
- a role can be assigned to a group for shared access

This split supports both individual access handling and reusable group-based administration.

## What Stays Owned By Keycloak

Keycloak remains the owner of:

- authentication
- login flows
- credential enforcement
- password policies
- token issuance
- session handling
- identity-provider behavior inside the realm

Fabric does not replace Keycloak. Fabric administers a defined part of the tenant realm.

## Tenant Isolation

The integration is designed around tenant isolation.

Important consequences:

- one tenant's Keycloak configuration does not affect another tenant
- user management actions are performed only against the configured realm for that tenant
- disabling the integration turns off Fabric-driven Keycloak administration for that tenant

## Operational Concepts

From a Fabric operator point of view, the Keycloak integration supports these workflows:

- create and maintain users
- create and maintain roles
- create and maintain groups
- add or remove users from groups
- assign or remove roles from users
- assign or remove roles from groups
- reset user passwords

These workflows let Fabric manage tenant access administration in Keycloak without requiring operators to use the Keycloak admin console for routine user and access changes.

## Boundary To Future Realm Provisioning

Tenant user administration and realm provisioning are related but not the same lifecycle.

Tenant user administration answers questions like:

- who can sign in
- which groups they belong to
- which roles they receive
- whether their password must be changed

Realm provisioning answers questions like:

- how a tenant realm is initialized
- which baseline roles or groups exist
- which clients or defaults are provisioned
- which tenant operating model the realm starts with

This document covers tenant-scoped administration only.

## External References

Keycloak REST API documentation:

- https://www.keycloak.org/docs-api/latest/rest-api/index.html

Keycloak OpenAPI reference:

- https://www.keycloak.org/docs-api/latest/rest-api/openapi.json
