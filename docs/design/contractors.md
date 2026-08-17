# Contractors

`Contractors` is the bounded context for persistent external worker planning.

It answers:

```text
Which contractors from which company are assigned to which work at which location and during which time window?
```

## Ownership

- `Contractors` owns contractor companies, contractors, job types, jobs, and job assignments.
- `Locations` owns the physical hierarchy.
- `Identities` owns canonical identity records and contractor-to-identity affiliation.
- `Requirements` owns enforcement zones, requirement policy, and compliance.
- `Reception` owns expected arrivals and expected offboard windows.

## Operational Roles

`Contractors` has two distinct operational roles over the same bounded context.

### ContractorEnrollment

`ContractorEnrollment` manages contractor master data.

Can:

- create companies
- edit companies
- create contractors
- edit contractors
- view jobs and assignments when needed for context

Cannot:

- create jobs
- edit jobs
- create job assignments
- edit job assignments

### ContractorPlanning

`ContractorPlanning` manages contractor work planning.

Can:

- create jobs
- edit jobs they own
- create job assignments on jobs they own
- edit job assignments on jobs they own
- list contractors for assignment selection
- view contractor details needed for assignment planning

Cannot:

- create companies
- edit companies
- create contractors
- edit contractors

This split keeps contractor/company master data separate from work planning responsibility.

The context stays location-based.

- `ContractorJob` references `LocationId`.
- `Contractors` does not resolve ancestor paths or enforcement zones itself.
- Downstream contexts such as `Requirements` and `Reception` consume contractor planning facts as needed.

## V1 Model

Implemented v1 concepts:

```text
Company
- Id
- Code
- Name
- CompanyNumber
- IsActive

Contractor
- Id
- CompanyId
- FirstName
- LastName
- Email
- ArchivedAt

JobType
- Id
- Code
- Name
- Description
- IsActive

ContractorJob
- Id
- CompanyId
- JobTypeId
- LocationId
- CreatedByIdentityId
- Name
- Description
- PlannedStart
- PlannedEnd
- Status

ContractorJobAssignment
- Id
- ContractorJobId
- ContractorId
- AssignedFrom
- AssignedUntil
- Status
```

## Identity Linkage Boundary

`Contractors` is not the source of truth for canonical identity linkage.

V1 rule:

- contractor-to-identity linkage stays in `Identities.ContractorAffiliation`
- `Contractors` may request the link to be created
- `Contractors` should not add its own persistent `IdentityId` source of truth

Why this matters:

- avoids duplicate ownership for canonical person linkage
- keeps identity lifecycle concerns centralized in `Identities`
- gives `Requirements`, `Reception`, and other contexts one stable identity link path to consume

## Aggregate Boundary

V1 uses `ContractorJob` as the aggregate root for assignments.

Why:

- the strongest local invariant is `AssignedUntil <= PlannedEnd`
- modeling `ContractorJobAssignment` as a child entity keeps that rule inside one aggregate
- this avoids cross-aggregate coordination for the most common write path

This is a deliberate simplicity choice for v1, not a statement that assignments can never be promoted later.

## Core Rules

- `Contractor` is persistent and not recreated per visit.
- `Contractor` belongs to one current `Company`.
- `Company` can have many contractors.
- `Company` can have many contractor jobs.
- one `ContractorJob` has exactly one `JobType`.
- one `ContractorJob` points to one `LocationId`.
- one `ContractorJobAssignment` links one contractor to one job for one assignment window.
- `ContractorJobAssignment.AssignedUntil` must not be after `ContractorJob.PlannedEnd`.
- a contractor assigned to a job must belong to the same company as the job.
- `ContractorJob` stores `CreatedByIdentityId`.
- planner-scoped job visibility is limited to jobs created by that planner.
- assignment visibility and mutation rights inherit from the parent job ownership.

## Lifecycle Semantics

V1 lifecycle behavior:

- `Company` can be activated or deactivated.
- `JobType` can be activated or deactivated.
- `Contractor` can be archived or unarchived.
- `ContractorJob` can move through `Planned`, `Active`, `Completed`, `Cancelled`.
- `ContractorJobAssignment` can move through `Planned`, `Active`, `Completed`, `Cancelled`.

When a job is completed or cancelled, open assignments are forced to the same terminal state.

## Planning Ownership

Planning ownership is defined at the job level.

Rules:

- `ContractorJob.CreatedByIdentityId` is set when the job is created
- a planner sees only jobs where `CreatedByIdentityId == current actor identity`
- a planner can edit only jobs they own
- a planner can create, edit, list, and cancel assignments only for jobs they own
- assignment ownership is not tracked separately; it follows the parent job

This matches the current aggregate boundary where `ContractorJob` owns its assignments.

## Integration Notes

### Requirements

`Requirements` should consume contractor planning facts, not own them.

- contractor jobs remain location-based in `Contractors`
- `Requirements` resolves `LocationId` to enforcement zones
- contractor requirement evaluation may use active assignment windows plus active job types per zone

### Reception

`Reception` remains the owner of expected arrivals.

V1 deliberately does not project contractor jobs or assignments into `Reception` automatically.

That projection should happen later through an explicit saga or application service once arrival-side requirements are defined.

## Deferred Work

Explicitly out of v1:

- company requirement policy
- enforcement-zone logic in `Contractors`
- requirement compliance evaluation
- reception expected-arrival projection
- contractor lifecycle saga behavior
- contractor-specific frontend CRUD pages

## Authorization Guidance

Recommended endpoint split:

- company CRUD -> requires `ContractorEnrollment`
- contractor CRUD -> requires `ContractorEnrollment`
- contractor list/detail for planning selection -> requires `ContractorPlanning` or `ContractorEnrollment`
- job CRUD -> requires `ContractorPlanning`
- job-assignment CRUD -> requires `ContractorPlanning`

Recommended query scoping:

- planner job list -> own jobs only
- planner job detail/update -> only owned jobs
- planner assignment list/detail/update -> only through owned jobs

## Why Future Engineers Should Care

This boundary should not need to be rediscovered.

The durable decisions are:

- `Contractors` owns planning facts
- `Identities` owns canonical person linkage
- `Locations` owns hierarchy
- `ContractorJob` owns assignments in v1 to keep time-window rules local

Future work should extend these boundaries rather than re-litigating them for each feature.
