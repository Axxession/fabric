# Grant Compliance Refactor Todo

Temporary implementation note. Tracks code refactors needed to align current code with the updated `Requirements` and `AccessCatalog` docs.

## Goal

Move from:

```text
grant created -> immediately provisionable unless revoked
```

to:

```text
grant created -> requirements attached once
grant compliance evaluated from attached requirements
provision only when approval + compliance allow it
```

## Current Gaps

Current code still assumes a created grant is active and provisioning should start immediately.

Key examples:

- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessGrant.cs`
  - grant always starts with `Status = Active`
- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessCatalogEnums.cs`
  - grant only has `Active` and `Revoked`
- `src/backend/Fabric.Server/AccessCatalog/Application/AccessGrantService.cs`
  - every create call immediately enqueues provisioning saga
- `src/backend/Fabric.Server/Sagas/AccessGrantProvisioning/AccessGrantProvisioningSagaService.cs`
  - provisioning checks only `Revoked`, not approval/compliance gates
- request approval flow creates grants after approval, but there is no distinct compliance gate before provisioning
- automatic grant sagas create/revoke grants, but no requirement derivation or replacement metadata exists

## Required Refactors

## 1. Expand AccessGrant Domain Model

Update `AccessGrant` to support the new business lifecycle.

Needed changes:

- add immutable location field directly on grant instead of relying only on `AccessGrantLocation`
- add replacement linkage:
  - `ReplacedById?`
- expand status model:
  - `Planned | Active | Revoked | Replaced | Expired`
- add approval gate:
  - `NotRequired | Pending | Approved | Rejected`
- add compliance gate:
  - `Compliant | TemporarilyCompliant | NonCompliant`
- add `CompliantUntil`
- add `LastComplianceEvaluatedAt`
- keep validity window mutable in place
- forbid in-place relocation or package/access reassignment

Likely files:

- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessGrant.cs`
- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessCatalogEnums.cs`
- `src/backend/Fabric.Server/AccessCatalog/Persistence/Configuration/AccessGrantConfiguration.cs`
- `src/backend/Fabric.Server/AccessCatalog/Contracts/AccessCatalogContracts.cs`
- migrations for AccessCatalog DB

## 2. Replace Or Remove AccessGrantLocation Join Model

Docs now treat grant location as part of the grant basis, not a mutable multi-location projection.

Need decision in code:

- either remove `AccessGrantLocation` entirely and store one `LocationId` on `AccessGrant`
- or keep table temporarily during migration, but constrain grants to exactly one location row

Current files:

- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessGrantLocation.cs`
- `src/backend/Fabric.Server/AccessCatalog/Persistence/Configuration/AccessGrantLocationConfiguration.cs`
- all query code reading `AccessGrantLocations`

## 3. Add Grant Requirement Attachment Model

Need persistence for requirements derived once at grant creation time.

Add entities similar to:

```text
GrantRequirement
GrantRequirementResult
```

Needed fields:

- `AccessGrantId`
- `RequirementDefinitionId`
- source policy reference
- blocking flag
- derivation timestamp
- per-requirement evaluation status
- evidence ref
- per-requirement valid-until

Likely files to add:

- new domain entities under `AccessCatalog/Domain/`
- new EF configuration under `AccessCatalog/Persistence/Configuration/`
- new DTO mappings under `AccessCatalog/Contracts/`
- migration

## 4. Introduce Requirement Derivation Service Boundary

At grant creation, `AccessCatalog` must derive attached requirements using `Requirements` policy.

Need application service contract such as:

```text
DeriveGrantRequirements(grant context) -> GrantRequirement[]
```

Context includes:

- identity
- subject kind
- location
- validity window
- source kind/source id
- contractor job types if relevant

Likely implementation areas:

- new service under `Requirements/Application/`
- integration path from `AccessGrantService`
- possibly saga-side enrichment for contractor/job context

## 5. Introduce Grant Compliance Evaluation

Need evaluator path that recalculates compliance only for requirements already attached to the grant.

Target output:

- per requirement result state
- grant `ComplianceStatus`
- grant `CompliantUntil`

Needed service shape:

```text
EvaluateGrantCompliance(accessGrantId) -> summary + requirement results
```

This should be called:

- after grant creation
- after relevant evidence changes
- after relevant external check results
- after relevant escort presence changes
- after automated grant validity changes when timing-sensitive requirements exist

## 6. Stop Immediate Provisioning On Grant Creation

Current create flow always enqueues provisioning.

Need change:

- create grant
- derive grant requirements
- evaluate grant compliance
- enqueue provisioning only if grant is currently provisionable
- otherwise keep grant withheld

Files likely affected:

- `src/backend/Fabric.Server/AccessCatalog/Application/AccessGrantService.cs`
- `src/backend/Fabric.Server/Sagas/AccessGrantProvisioning/AccessGrantProvisioningSagaService.cs`
- `src/backend/Fabric.Server/Sagas/AccessGrantProvisioning/AccessGrantProvisioningSaga.cs`

Need a provisionable check based on:

- grant lifecycle state
- approval status
- compliance status
- validity window

## 7. Rework Approval Flow Integration

Current request flow effectively treats approval completion as enough to provision.

Need new rule:

- approval completion creates grant with `ApprovalStatus = Approved`
- grant may still be `NonCompliant`
- provisioning waits for both approval and compliance

Affected files:

- `src/backend/Fabric.Server/AccessCatalog/Application/ApprovalDecisionService.cs`
- `src/backend/Fabric.Server/AccessCatalog/Application/PackageRequestService.cs`

## 8. Rework Automatic Grants

Automatic grants currently bypass approval and create active grants immediately.

Need new behavior:

- automatic grants created with `ApprovalStatus = NotRequired`
- requirements derived at creation time
- compliance evaluated immediately
- provisioning withheld until compliant

Potential source kinds to review:

- employee OU
- employee persona
- reception arrival / visitor / contractor triggers
- future contractor job automation source kinds to add

Current files:

- `src/backend/Fabric.Server/Sagas/EmployeeLifecycle/EmployeeLifecycleAutomationService.cs`
- `src/backend/Fabric.Server/Reception/Application/ReceptionTriggeredPackageAssignmentService.cs`
- `src/backend/Fabric.Server/AccessCatalog/Domain/AccessCatalogEnums.cs`

## 9. Add Grant Replacement Support

Grant domain should support replacement metadata, but not decide when replacement is needed.

Need domain operations for:

- update validity
- mark replaced by successor grant
- revoke

Saga/process managers decide whether source changes cause replacement.

Likely implementation points:

- `AccessGrant` aggregate methods
- employee lifecycle automation reconciliation
- reception-triggered package assignment service
- future contractor/job automation services

## 10. Rework Provisioning Saga State Model

Current provisioning saga tracks technical materialization only.

Need to separate:

- grant approval/compliance eligibility
- technical provisioning work

At minimum:

- skip provisioning for non-provisionable grants without treating it as a failure
- revoke or retract assignments when grant becomes non-compliant
- provision until `min(ValidUntil, CompliantUntil)`

Files:

- `src/backend/Fabric.Server/Sagas/AccessGrantProvisioning/AccessGrantProvisioningSagaService.cs`
- `src/backend/Fabric.Server/Sagas/AccessGrantProvisioning/AccessGrantProvisioningSaga.cs`

## 11. Add Event Triggers For Compliance Recalculation

Need workers, handlers, or saga triggers for grant compliance reevaluation when relevant evidence changes.

Trigger sources likely include:

- requirement evidence created
- requirement evidence revoked or expired
- requirement evidence check completed
- escort presence changed
- automated grant validity changed

Need a way to efficiently find affected grants by attached `RequirementDefinitionId` and `IdentityId`.

## 12. Add Manual Administrative Recalculation Operation

Docs now define explicit recalculation of grant requirements after policy changes.

Need admin operation such as:

```text
RecalculateGrantRequirements(futureOnly: bool)
```

Suggested first implementation:

- future-only scope first
- optional later filters by package, location subtree, or source kind

## 13. Update APIs And UI Read Models

Expose new grant lifecycle and compliance fields.

Needed API changes:

- list grants should include approval/compliance state
- grant detail should include attached requirements and current results
- package request detail should show approved-but-non-compliant grants clearly

Current contracts to revisit:

- `src/backend/Fabric.Server/AccessCatalog/Contracts/AccessCatalogContracts.cs`
- access grant endpoints
- package request endpoints

## 14. Add Migration Strategy

Existing data uses:

- `AccessGrantStatus.Active | Revoked`
- `AccessGrantLocation` join rows
- no requirement attachments
- no compliance fields

Need migration plan for live data:

- backfill `ApprovalStatus`
- backfill `ComplianceStatus`
- decide initial default for existing grants
- decide whether existing grants get empty requirement sets or require one-time derivation
- decide how to map existing location rows to new single grant location field

## Suggested Implementation Order

1. Expand domain model + enums + persistence.
2. Add grant requirement entities and migrations.
3. Add derivation contract from `Requirements`.
4. Add compliance evaluation service.
5. Update grant creation flow.
6. Update provisioning saga to respect approval/compliance.
7. Update automatic-grant sagas and request flows.
8. Add recalculation triggers.
9. Add admin recalculation operation.
10. Update API responses and UI consumers.

## Open Code Decisions

- Should `AccessGrantLocation` be removed immediately or phased out?
- Should grant replacement be modeled as a new status only, or status plus `ReplacedById`?
- Should compliance evaluation live inside `Requirements` application services or inside an `AccessCatalog` orchestrator that calls `Requirements` evaluators?
- How much of per-requirement result history is needed in v1 versus current-state only?
