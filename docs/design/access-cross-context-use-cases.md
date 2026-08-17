# Access Cross-Context Use Cases

## Catalog Request: Warehouse Access For Antwerp

Setup:

```text
Locations:
- Site Lille
- Site Antwerp

Access Control:
- PACS FR linked to Site Lille
- PACS BE linked to Site Antwerp
- AccessItem Warehouse
- AccessLevelTarget in PACS FR: Warehouse Lille
- AccessLevelTarget in PACS BE: Warehouse Antwerp

Access Catalog:
- ApprovalGroup Facility Managers
- Sverre responsible for Site Antwerp
- Kris responsible for Site Lille
- Package Warehouse contains AccessItem Warehouse
```

Request:

```text
Requester: Dimitar
Beneficiary: Dimitar
Package: Warehouse
Requested locations:
- Site Antwerp
Reason: Needs warehouse access for inventory work.
```

Resolution:

```text
Site Antwerp -> nearest PACS link -> PACS BE
Package Warehouse -> AccessItem Warehouse
AccessItem Warehouse + PACS BE + Site Antwerp -> AccessLevelTarget Warehouse Antwerp
Facility Managers + Site Antwerp hierarchy -> Sverre
```

Result:

```text
Sverre approves.
After approval, Fabric creates the access grant.
If the grant is compliant, Access Control creates a PACSAssignment for PACS BE / Warehouse Antwerp.
If the grant is not yet compliant, provisioning is withheld until compliance is satisfied.
PACS FR / Warehouse Lille is not involved.
Kris is not asked to approve unless organizational approval requires L+2.
```

## One PACS With Building-Specific Targets

Setup:

```text
Locations:
- Site Leuven
  - Building A
  - Building B

Access Control:
- PACS BE linked to Site Leuven
- AccessItem IT Staff
- AccessLevelTarget:
  - LocationId: Building A
  - PACS: BE
  - Native target: IT Building A
- AccessLevelTarget:
  - LocationId: Building B
  - PACS: BE
  - Native target: IT Building B
```

Flow:

```text
Grant location = Building A
-> nearest PACS link = PACS BE
-> best target scope in PACS BE = Building A
-> assign native target IT Building A
```

## Multiple Targets At The Same Winning Scope

Setup:

```text
AccessItem Warehouse
PACS BE
Grant location: Site Antwerp

Targets at Site Antwerp scope:
- Warehouse Doors
- Warehouse Turnstiles
```

Flow:

```text
Site Antwerp wins as best scope
-> all enabled targets at Site Antwerp scope apply
-> create multiple PACSAssignments for the same grant reason
```

## Automatic Employee Assignment From Organizational Unit

Setup:

```text
EmployeeLifecycleSaga rule:
- OrganizationUnitId: Warehouse Operators
- PackageId: Warehouse
- LocationId: Site Antwerp
```

Flow:

```text
Employee joins Warehouse Operators.
EmployeeLifecycleSaga grants Warehouse package with AssignmentChannel AutomaticConfiguration.
Approvals are bypassed.
Grant requirements are attached at grant creation time.
If the grant is compliant, Access Control resolves Site Antwerp to the nearest PACS link: PACS BE and creates a PACSAssignment for PACS BE / Warehouse Antwerp.
If not compliant, the grant exists but provisioning is withheld.
```

## Employee Lifecycle Change

Setup:

```text
Employee:
- ContractEndDate becomes yesterday
- Previous calculated status: Active
- New calculated status: Terminated
```

Flow:

```text
EmployeeLifecycleSaga records EmployeeLifecycleEvent Active -> Terminated.
EmployeeLifecycleSaga revokes automatic access grants for the employee.
CredentialManagement revokes employee credentials.
Access Control retracts/supersedes related PACS assignments.
```

Leave and suspension:

```text
Leave
- credentials may be temporarily suspended for the leave window
- package grants can remain but PACS assignments may be inactive depending on policy

Suspended
- credentials are suspended immediately
- PACS assignments are suspended or retracted immediately
```

## Visitor Assignment From Visit Location

Setup:

```text
VisitorAccessAutomation rule:
- LocationId: Site Antwerp
- PackageId: Visitors
```

Flow:

```text
Visit is created for Site Antwerp.
VisitorAccessAutomation requests a visitor credential from Credential Management.
VisitorAccessAutomation grants Visitors package with AssignmentChannel AutomaticConfiguration.
Approvals are bypassed.
Grant requirements are attached at grant creation time.
If the grant is compliant, Access Control resolves Site Antwerp to the nearest PACS link: PACS BE and assigns the visitor access targets.
If not compliant, the grant remains withheld until compliance is satisfied.
```

## Dependency Direction

Use id references between bounded contexts:

```text
AccessControl -> Locations by LocationId
AccessCatalog -> Locations by LocationId
AccessCatalog -> AccessControl by AccessItemId
CredentialManagement -> AccessControl by CredentialTypeTarget and location-based provisioning resolution
Automation/Sagas -> Employees, Visitors, AccessCatalog, CredentialManagement by application-service calls
```

Avoid cross-context ownership:

- Locations does not own PACS coverage.
- Access Control does not own approvals or package requests.
- Access Catalog does not own native PACS objects or PACS assignments.
- Credential Management does not own access items or packages.
- Employees and Visitors do not own automatic access package grant rules.
