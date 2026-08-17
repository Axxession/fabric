# Access Domain Overview

This design set summarizes the bounded-context split for locations, contractors, PACS access control, access catalog packages, automation/sagas, and credential management.

Updated ubiquitous language:

- `AccessItem`: our global business access concept, previously called Access Level.
- `Package`: the requestable business bundle, previously called Business Access Package.
- `AccessLevelTarget`: the native PACS access-level/access-rule mapping.
- `CredentialTypeTarget`: the native PACS credential-type/system mapping.
- `PACSAssignment`: the technical provisioning item, previously called provisioning transaction.

The core separation is:

- `Locations` owns where things are.
- `AccessControl` owns PACS infrastructure, access items, native PACS mappings, and technical PACS assignments.
- `AccessCatalog` owns catalogs, packages, requests, grants, approvals, approval groups, grant-attached requirements, and grant compliance state.
- `Contractors` owns contractor companies, contractors, contractor job types, contractor jobs, and contractor job assignments.
- `Employees` owns employee records, organization units, manager hierarchy, and calculated employee lifecycle.
- `CredentialManagement` owns credential types, numbers, issued credentials, and credential PACS assignments.
- Automation/saga contexts own cross-boundary rules such as OU-to-package or visitor-location-to-package.

Design files:

- `locations.md`
- `access-control.md`
- `access-catalog.md`
- `contractors.md`
- `employees.md`
- `actors.md`
- `automation-sagas.md`
- `credential-management.md`
- `access-cross-context-use-cases.md`

Dependency direction summary:

```text
AccessControl -> Locations by LocationId
AccessCatalog -> Locations by LocationId
AccessCatalog -> AccessControl by AccessItemId and provisionable grant output
AccessCatalog -> Requirements by requirement derivation and grant compliance evaluation
Contractors -> Locations by LocationId
Requirements -> Contractors by contractor planning facts and JobTypeId inputs used during contractor requirement derivation
CredentialManagement -> AccessControl by CredentialTypeTarget and location-based provisioning resolution
Automation/Sagas -> Employees, Visitors, AccessCatalog, CredentialManagement by application-service calls
```

Avoid cross-context ownership:

- Locations does not own PACS coverage.
- Access Control does not own approvals or package requests.
- Access Catalog does not own native PACS objects or PACS assignments.
- Access Catalog does not own requirement policy or evidence.
- Contractors does not own identity linkage, requirement policy, grant-attached requirements, or grant compliance state.
- Credential Management does not own access items or packages.
- Employees and Visitors do not own automatic access package grant rules.
