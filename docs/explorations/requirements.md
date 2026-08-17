# Requirements

This document describes the `Requirements` bounded context after moving grant-attached compliance state into `AccessCatalog`.

## Core Separation

- `Requirements` owns requirement definitions, location-scoped requirement policy, evaluator behavior, and evidence.
- `Requirements` answers which requirements apply for a given grant context and whether attached grant requirements are currently satisfied.
- `AccessCatalog` owns packages, grants, approvals, grant-attached requirements, and grant compliance status.
- `Locations` owns the physical location hierarchy.
- `Contractors` owns contractor companies, contractors, jobs, assignments, and job types.
- `ReceptionDesk` owns expected arrivals, onboarding state, and expected end plus grace.
- `AccessControl` owns PACS mappings and technical provisioning.
- automation/application services coordinate grant creation, compliance reevaluation, and provisioning.

`Requirements` no longer owns a long-lived computed compliance record per identity and perimeter. Instead it provides the policy and evaluation model used by `AccessCatalog` grants.

## Purpose

The context exists to answer two questions:

```text
1. Which requirements apply for this grant context right now?
2. For this already-attached grant requirement set, which requirements are currently satisfied and until when?
```

The context must support:

- requirements attached to a location and inherited by descendants
- employee, visitor, and contractor requirement variants
- extra contractor requirements driven by active job type
- multiple evidence mechanisms, not only uploaded documents
- pre-arrival evaluation so future grants can be provisioned when they become compliant
- short-lived and continuous requirements such as escort presence

## Core Domain Rules

- requirement policy is location-based
- a requirement attached to a location applies to that location and every descendant location
- effective requirements for a target location are the union of the target location plus all ancestors
- employees and visitors use only location requirement policy
- contractors use location requirement policy plus extra job-type requirements
- requirement derivation for a grant happens once at grant creation time
- later policy changes do not automatically change existing grants
- grant compliance is recalculated against the grant's attached requirements, not against live policy
- policy changes may still be applied later through an explicit grant-recalculation operation in `AccessCatalog`

## Location Requirement Policy

The physical location hierarchy is the policy scope.

Recommended shape:

```text
LocationRequirementPolicy
- Id
- LocationId
- RequirementDefinitionId
- SubjectKind
- IsBlocking
- IsEnabled
```

Subject examples:

- `Employee`
- `Visitor`
- `Contractor`
- `Any`

Resolution rules:

- start from the grant target `LocationId`
- walk up the ancestor path
- collect all enabled `LocationRequirementPolicy` rows whose `SubjectKind` matches the evaluated subject or `Any`
- if no policy is found on the path, the grant has no attached requirements and is compliant by default

Example:

```text
Location tree:
- Site BNP
  - Building A
    - IT Server Room

Policies:
- Site BNP -> site_safety_training for Any
- IT Server Room -> escort_required for Visitor

Effective requirements for Visitor at IT Server Room:
- site_safety_training
- escort_required
```

## Contractor Job Requirement Policy

Contractors may have extra requirements based on active job type at the grant location context.

Recommended shape:

```text
LocationJobRequirementPolicy
- Id
- LocationId
- JobTypeId
- RequirementDefinitionId
- IsBlocking
- IsEnabled
```

Resolution rules:

- use the same target location ancestor path resolution as location requirement policy
- collect active contractor `JobTypeId` values relevant to the grant context
- union all matching enabled `LocationJobRequirementPolicy` rows

Examples:

- Site Antwerp + `Welding` -> `hot_work_training`
- Building A + `Electrical` -> `electrical_safety_certificate`

## Requirement Definition

`RequirementDefinition` represents one business requirement meaning.

Examples:

- `site_safety_training`
- `vca_certified`
- `not_ocad_blacklisted`
- `escort_required`
- `max_hours_per_day`

Recommended shape:

```text
RequirementDefinition
- Id
- Code
- Name
- Description
- EvaluatorKind
- IsSensitive
- IsActive
```

Evaluator examples:

- `UploadedDocument`
- `ExternalCheck`
- `Escort`
- `Computed`

## Requirement Evidence

`RequirementEvidence` is the factual evidence model used by evaluators.

It covers:

- uploaded certificates and documents
- imported or manually attested proof
- dynamic external check results
- computed evidence outcomes
- escort-presence-backed evidence

Recommended shape:

```text
RequirementEvidence
- Id
- IdentityId
- RequirementDefinitionId
- EvidenceKind
- Status
- ValidFrom
- ValidUntil
- SourceReference
- Summary
- IsSensitive
- VerifiedAt
```

Important rule:

- uploaded certificates stay inside `Requirements`; they are not a separate bounded context

## Requirement Evidence Check

`RequirementEvidenceCheck` stores operational work state for dynamic evaluators.

Recommended shape:

```text
RequirementEvidenceCheck
- Id
- IdentityId
- RequirementDefinitionId
- Status
- RequestedAt
- CompletedAt
- AttemptCount
- LastKnownError
- ResultEvidenceId
```

Important rule:

- `RequirementEvidenceCheck` stores operational work state
- `RequirementEvidence` stores the factual result used for grant compliance evaluation

## Escort Use Case

Escort remains a normal requirement definition with a special evaluator.

Recommended setup:

```text
RequirementDefinition
- Code: escort_required
- EvaluatorKind: Escort
```

Recommended operational model:

```text
EscortPresence
- Id
- LocationId
- EscortIdentityId
- EscortedIdentityId
- Status
- StartedAt
- ValidUntil
- EndedAt
```

Behavior:

- entry-only escort can produce short `ValidUntil`
- continuous escort can keep `ValidUntil` aligned to the active presence
- when escort evidence expires, grants attached to `escort_required` become `TemporarilyCompliant` or `NonCompliant` depending on grant duration

## Grant Requirement Derivation

When a grant is created, `AccessCatalog` derives and attaches its effective requirement set using `Requirements` policy.

Derivation inputs:

- `IdentityId`
- `SubjectKind`
- target `LocationId`
- grant validity window
- grant source context
- active contractor job types when subject is contractor

Derivation outputs:

- the effective requirement set for that grant at creation time
- metadata describing which policy rows caused each attached requirement

Important rules:

- derivation happens once when the grant is created
- policy changes do not automatically mutate attached grant requirements
- later manual operations may explicitly recalculate future grants if policy changed

## Grant Compliance Evaluation

`Requirements` evaluators compute compliance for the requirement set attached to a grant.

Evaluation outputs consumed by `AccessCatalog`:

- per-grant requirement result status
- earliest expiry across currently satisfied requirements
- business compliance state for the grant

Recommended grant-side statuses:

- `Compliant`
- `TemporarilyCompliant`
- `NonCompliant`

Recommended meaning:

```text
Compliant
- all attached requirements are satisfied for the full remaining grant duration

TemporarilyCompliant
- all attached requirements are satisfied now
- but the earliest requirement expiry is before grant end

NonCompliant
- one or more attached blocking requirements are currently unsatisfied
```

Recommended date output:

```text
CompliantUntil
- null when the grant is compliant for its full duration
- the earliest fulfilled attached requirement expiry otherwise
```

## Recalculation Triggers

Attached grant requirements are not re-derived automatically, but their compliance result is recalculated when relevant facts change.

Primary triggers:

- evidence added for a requirement attached to the grant
- evidence removed, revoked, rejected, or expired for a requirement attached to the grant
- external check result arrived for a requirement attached to the grant
- escort presence started, ended, or expired for a requirement attached to the grant
- automated grant validity changed and one or more attached evaluators depend on grant timing

Policy changes do not automatically trigger re-derivation.

## Boundary Rules

- `Requirements` owns requirement definitions, location-based requirement policy, evidence, and evaluator behavior.
- `Requirements` consumes contractor planning facts from `Contractors` to resolve active job types for derivation.
- `Requirements` consumes arrival/onboarding timing from `ReceptionDesk` when required by evaluators.
- `Requirements` does not own packages, grants, approvals, or grant replacement.
- `Requirements` does not own PACS-native provisioning.
- `AccessCatalog` owns grant-attached requirements and grant compliance status derived from `Requirements` evaluation.

## Example: Employee Request

```text
Employee requests package for Building A.
Grant is created after approval.
Requirements are derived from Building A + ancestor locations.
Attached requirements:
- site_safety_training
- badge_photo_uploaded

Evidence exists for site_safety_training only.
Grant compliance:
- NonCompliant
```

If badge photo evidence is later added:

```text
Grant compliance recalculates.
Result:
- Compliant or TemporarilyCompliant depending on earliest expiry versus grant end
```

## Example: Contractor Automatic Grant

```text
Contractor job Mon-Fri at Site Antwerp, JobType Welding.
Automation creates grant.
Approval not required.
Requirements are derived once from:
- Site Antwerp location path
- Contractor subject kind
- Welding job type

Attached requirements:
- site_safety_training
- not_ocad_blacklisted
- hot_work_training
```

If OCAD and training are valid until Thursday but the grant lasts until Friday:

```text
Compliance status: TemporarilyCompliant
CompliantUntil: Thursday 18:00
```

## Example: Policy Change

```text
Policy at Site Antwerp adds chemical_training.
Existing grants are unchanged.
Future grants derive chemical_training automatically.

If operations want old future grants updated too:
- run explicit grant requirement recalculation in AccessCatalog
```

## Open Decisions

- Should `GrantRequirement` store only policy references, or also a derivation context snapshot for audit?
- Should non-blocking requirements appear in grant compliance UX even though they do not block provisioning?
- Should manual requirement recalculation support filtering by package, location subtree, or source kind in v1?
