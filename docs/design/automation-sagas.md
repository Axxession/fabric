# Automation / Sagas

Automatic package assignment rules belong in automation/saga contexts that coordinate source-domain lifecycle events with Access Catalog grants.

Examples:

```text
EmployeeLifecycleSaga
- owns OU-to-package and persona-to-package rules
- owns employee lifecycle access policy
- reacts to employee OU/status/lifecycle/persona/work-location changes
- calls AccessCatalog to grant/revoke packages
- calls CredentialManagement to suspend/revoke credentials when lifecycle requires it
- calls AccessControl to block/unblock PACS subjects for leave or suspension
```

```text
VisitorAccessAutomation
- owns reception/visitor trigger-to-package rules
- reacts to visit lifecycle
- calls CredentialManagement to issue visitor credentials
- calls AccessCatalog to grant/revoke packages
```

## Reception / Visitor Trigger Rules

The current reception access matrix is conceptually a visitor access automation rule.

Current shape:

```text
ReceptionAccessRuleAssignment
- LocationId
- Trigger
- SystemId
- AccessLevelTypeId
- GracePeriodMinutes
```

New shape:

```text
VisitorAccessAutomationRule
- Trigger
- PackageId
- ValidityOffsetBefore
- ValidityOffsetAfter
- LocationId?
- IsEnabled
```

The rule should usually not point to a PACS or native access level. It grants a package. The arrival or visit location determines which PACS and access-level target are relevant once the grant is compliant and provisionable.

Example global rule:

```text
VisitorConfirmed -> Package Visitors
```

Execution:

```text
Arrival.LocationId = Site Antwerp
Package Visitors -> AccessItem Visitor Access
Site Antwerp -> nearest PACS link -> PACS BE
AccessItem Visitor Access + PACS BE -> AccessLevelTarget Visitor Antwerp
If the grant is compliant, Access Control creates PACSAssignment
```

`LocationId` on the automation rule is optional:

- `LocationId = null`: rule applies globally.
- `LocationId = X`: rule applies only when the arrival location is inside X's location tree.

Use a scoped location rule only when different locations need different packages for the same trigger.

Examples:

```text
Global:
- VisitorConfirmed -> Package Visitors
```

```text
Scoped:
- Site Antwerp + VisitorConfirmed -> Package Visitors Antwerp Special
- Warehouse Building + VisitorConfirmed -> Package Warehouse Escort Required
```

Validity offsets replace the current grace-period concept:

```text
ValidFrom = Arrival.ExpectedArrivalTime - ValidityOffsetBefore
ValidUntil = Arrival.ExpectedOffboardTime + ValidityOffsetAfter
```

Reception should own arrival lifecycle facts and triggers. Visitor access automation should own the rule that translates those triggers into package grants.

Automation assignment source examples:

```text
Employee organizational unit rule:
- AssignmentChannel: AutomaticConfiguration
- SourceKind: OrganizationalUnit
- SourceId: OrganizationUnitId
```

```text
Employee persona rule:
- AssignmentChannel: AutomaticConfiguration
- SourceKind: Persona
- SourceId: PersonaId
```

```text
Visitor location matrix:
- AssignmentChannel: AutomaticConfiguration
- SourceKind: VisitorLocation
- SourceId: LocationId
```

Boundary rules:

- Employees owns employee lifecycle and organizational units, not access consequences.
- Visitors owns visitor and visit lifecycle, not access consequences.
- Access Catalog owns the grant command, grant compliance state, and package assignment lifecycle.
- Automation/sagas own the cross-context policy that decides when to call Access Catalog.

## Learning Requirement Rules

Learning/requirement coupling belongs in an automation or application-service seam, not inside `Learning` or `Requirements` ownership.

Recommended shape:

```text
LearningRequirementRule
- RequirementDefinitionId
- CourseId
- SatisfactionMode
- MinimumScore?
- IsEnabled
```

Current rule semantics:

- one requirement may map to one or more courses
- one course may satisfy one or more requirements
- the rule answers whether completion alone is enough or whether a minimum score is required

Important operating rule:

- the rule does not automatically create enrollments in the background
- completion surfaces choose when to inspect missing learning requirements in their current source context and when to offer courses

Current flow:

```text
completion surface resolves relevant missing learning requirements
-> surface resolves mapped courses through LearningRequirementRule
-> user selects course
-> application service upserts Enrollment in Learning
-> learner completes course
-> automation/application service writes RequirementEvidence
-> Requirements triggers compliance recalculation
```

## Employee Lifecycle PACS Subject State

Current simplified employee lifecycle policy:

- automatic `AccessGrant`s remain granted through lifecycle changes
- lifecycle automation does not currently revoke automatic grants
- lifecycle automation affects every already-linked `PACSSubject` for the employee identity
- no relevant-PACS calculation is performed; all linked `PACSSubject`s are controlled
- one setting exists for leave handling:
  - `DisableEmployeeOnLeave = true` -> leave blocks all linked `PACSSubject`s
  - `DisableEmployeeOnLeave = false` -> leave does nothing to PACS subject state

Current subject-state behavior:

```text
Active
- PACSSubject -> Active

Leave
- if DisableEmployeeOnLeave = true -> PACSSubject -> Blocked
- otherwise no subject-state change

PreHire
- PACSSubject -> Blocked

Suspended
- PACSSubject -> Blocked

Terminated
- PACSSubject -> Archived

Archived
- PACSSubject -> Archived
```

For temporary lifecycle states:

```text
AccessGrant remains granted.
AccessItem assignments remain known.
PACSSubject is blocked in each linked PACSSubject.
```

When the lifecycle state ends:

```text
PACSSubject is unblocked.
Existing PACS assignments become usable again.
```

`EmployeeLifecycleSaga` coordinates this by upserting `PACSSubjectProvisioning` records in Access Control for every linked `PACSSubject` of the employee identity.

Employee leave:

```text
EmployeeLifecycleEvent: Active -> Leave
EmployeeLifecycleSaga -> AccessControl:
- DesiredState: Blocked
- Reason: EmployeeLeave
- SourceKind: EmployeeLifecycleSaga
- SourceId: lifecycle event id
```

Employee suspension:

```text
EmployeeLifecycleEvent: Active -> Suspended
EmployeeLifecycleSaga -> AccessControl:
- DesiredState: Blocked
- Reason: EmployeeSuspension
- SourceKind: EmployeeLifecycleSaga
- SourceId: lifecycle event id
```

Leave or suspension ends:

```text
EmployeeLifecycleEvent: Leave/Suspended -> Active
EmployeeLifecycleSaga -> AccessControl:
- DesiredState: Active
- Reason: EmployeeLifecycleRestored
- SourceKind: EmployeeLifecycleSaga
- SourceId: lifecycle event id
```

Termination is currently simplified:

```text
EmployeeLifecycleEvent: Active -> Terminated
EmployeeLifecycleSaga:
- leaves automatic access grants unchanged
- commands AccessControl to archive all linked PACS subjects
```

## Employee OU, Persona And Work Location Rules

Current automatic grant sources:

```text
OrganizationalUnitPackageRule
- OrganizationUnitId
- PackageId
- IsEnabled
```

```text
PersonaPackageRule
- PersonaId
- PackageId
- IsEnabled
```

Resolution uses all current employee work locations and creates automatic grants per source:

```text
For each active employee
For each current employee work location
For each enabled OrganizationalUnitPackageRule matching employee.OrganizationUnitId
Grant Package for that Location with:
- AssignmentChannel: AutomaticConfiguration
- SourceKind: OrganizationalUnit
- SourceId: OrganizationUnitId

For each active employee
For each employee persona
For each current employee work location
For each enabled PersonaPackageRule
Grant Package for that Location with:
- AssignmentChannel: AutomaticConfiguration
- SourceKind: Persona
- SourceId: PersonaId
```

Example:

```text
Employee: Sverre
Personas:
- Facility Manager

Work locations:
- Site Leuven
- Site Brussels

PersonaPackageRule:
- Facility Manager -> Facility Manager Access
```

Resulting automatic assignments:

```text
Facility Manager Access / Site Leuven
Facility Manager Access / Site Brussels
```

The saga reconciles desired current-state grants against existing automatic grants:

```text
Desired grants =
  active employee
  x current work locations
  x enabled OU package rules
  +
  active employee
  x current personas
  x current work locations
  x enabled persona package rules

Reconciliation:
- missing desired grant -> create access grant and derive grant requirements
- existing automatic grant no longer desired -> revoke access grant
- desired grant already exists -> leave unchanged
```

When a sync adds or removes a work location, the employee domain records the current-state change and the saga cascades the access consequences.

## HR Sync Versus Lifecycle Automation

Employee processing has two separate facets.

Facet 1: HR/classifier sync to Employees.

```text
HR / Directory / Classifier
-> Employees
```

This maintains the latest employee facts:

- OU
- personas
- work locations
- contract dates
- leave periods
- suspension periods
- directory id and email

Facet 2: Employee lifecycle automation.

```text
Employees
-> EmployeeLifecycleSaga
-> AccessCatalog / CredentialManagement / AccessControl
```

This applies access and credential consequences from the current employee state.

`EmployeeLifecycleSaga` can be operationally disabled without pausing HR sync.

```text
EmployeeLifecycleAutomationSettings
- IsEnabled
- DisableEmployeeOnLeave
- DisabledAt
- ReenabledAt
- LastFullReconciledAt
```

When disabled:

- HR sync continues updating employee facts.
- Employee lifecycle events can still be recorded.
- No access grants are created or revoked.
- No PACS subject provisioning operations are created.
- No event buffering is required.

When re-enabled:

- Run a full reconciliation from current employee state.
- Do not replay every intermediate employee change.
- Recompute desired automatic grants and lifecycle actions from latest facts and current rules.

Example:

```text
Before disable:
- OU: Sales
- Persona: Office Staff
- Work location: Antwerp

During disable:
- OU becomes Warehouse
- Persona becomes Warehouse Staff
- Work location becomes Brussels
- Status remains Active

On re-enable:
- Ignore intermediate transition replay.
- Desired state is calculated from Warehouse + Warehouse Staff + Brussels + Active.
- Stale automatic grants are revoked/superseded.
- Missing desired grants are created.
```

This makes lifecycle automation reconciliation-based rather than event-buffer-based. The employee facts are the source of truth; automation side effects are made eventually consistent with those facts.
