# Learning

`Learning` owns SCORM course packages, course versions, launchable SCO structure, manual enrollments, attempts, launch sessions, SCORM progress, and course completion facts.

It owns:

- business courses
- uploaded SCORM package snapshots
- launchable SCO metadata derived from `imsmanifest.xml`
- learner enrollments
- learner attempts
- learner launch sessions
- persisted `ScormProgress`
- course completion facts and optional score capture

`Learning` does not own learner identity, requirement policy, evidence validity, pass thresholds, or retraining cadence. It consumes `IdentityId` from `Identities` and exposes completion facts for later integration into `Requirements`.

## Core Separation

- `Learning` owns course delivery and learner progress facts.
- `Identities` owns canonical identity records. `Learning` references `IdentityId` only.
- `Requirements` owns requirement definitions, evidence meaning, validity/expiry rules, and any required passing threshold.
- automation/application services may later coordinate requirement-driven enrollment and completion-to-evidence translation.
- SCORM is a delivery/runtime mechanism inside `Learning`, not a separate bounded context.

## Purpose

The `Learning` bounded context exists to answer:

```text
Which courses are available, which identities are enrolled in them, what attempt/progress state exists for each enrollment, and which course version produced the final completion fact?
```

The context must support:

- uploading valid SCORM packages
- parsing package manifests into course launch structure
- storing versioned course package metadata
- manual assignment of courses to identities
- multiple attempts within one active enrollment
- tracking SCORM runtime progress and final completion
- optional score capture when a course emits scores
- repeated training through a new enrollment after the prior enrollment is terminal
- future integration where a course completion can satisfy a requirement through a saga or application service

## Ubiquitous Language

`Course` is the stable business learning item shown to users and admins.

`CourseLanguage` is one delivery language variant of a course. It carries the language code, UI label, and current version pointer for that language.

`CourseVersion` is one uploaded SCORM package snapshot for one course language. It captures the exact package, manifest-derived launch structure, and runtime version used for learner attempts.

`SCO` is the SCORM technical launch unit. It is part of runtime delivery structure, not the primary business assignment unit.

`Enrollment` is one assigned learning cycle for one `IdentityId` and one `CourseId`.

`Attempt` is one learner run inside an enrollment.

`LaunchSession` is the short-lived runtime token/session used to open SCORM content for an attempt.

`ScormProgress` is the persisted normalized runtime state for an attempt.

Important distinction:

- `Enrollment` is the business assignment record.
- `Attempt` is the runtime execution record.
- `ScormProgress` is the persisted runtime state inside that execution.
- completion is a terminal outcome on an `Attempt` and rolled up onto the `Enrollment`.

## Course And Course Version

`Course` is the stable business anchor.

Recommended shape:

```text
Course
- Id
- Code
- Title
- Description
- CurrentVersionId?
- IsActive
- CreatedAt
- UpdatedAt
```

`CourseLanguage` owns the language-specific delivery branch.

Recommended shape:

```text
CourseLanguage
- Id
- CourseId
- LanguageCode
- DisplayLabel
- CurrentVersionId?
- IsActive
- CreatedAt
- UpdatedAt
```

`CourseVersion` stores the exact uploaded SCORM snapshot for one language.

Recommended shape:

```text
CourseVersion
- Id
- CourseId
- CourseLanguageId
- VersionNumber
- Title
- ScormVersion
- EmitsScore
- StorageLocation
- ManifestChecksum?
- PublishedAt
- CreatedAt
```

`CourseVersion` also owns the ordered SCO structure derived from the manifest.

Recommended shape:

```text
CourseSco
- Id
- CourseVersionId
- ScoIdentifier
- Title
- LaunchUrl
- ResourcePath
- ManifestOrder
- MasteryScore?
```

Versioning rules:

- `Course` remains the stable business identity across uploads.
- a course is created first, then one or more `CourseLanguage` rows are added, then versions are uploaded under a chosen language.
- an upload creates a new `CourseVersion` inside one `CourseLanguage` branch.
- version numbering is scoped per language, not globally per course.
- `CourseLanguage.CurrentVersionId` points to the active/current package for that language.
- every `Attempt` stores the `CourseVersionId` actually used at runtime.
- reports and history must be able to answer which version a learner completed.
- completing any language version still counts as completing the course globally.

## Enrollment

`Enrollment` is the business learning cycle.

Recommended shape:

```text
Enrollment
- Id
- CourseId
- IdentityId
- Status
- AssignedAt
- AssignedByIdentityId
- StartedAt?
- CompletedAt?
- CompletedAttemptId?
- LatestAttemptId?
- CancelledAt?
- CancelledByIdentityId?
- CancellationReason?
```

Recommended enum:

```text
EnrollmentStatus
- Assigned
- InProgress
- Completed
- Cancelled
```

Lifecycle rules:

- `Assigned` means the learner has a course assignment but no meaningful progress yet.
- `InProgress` means at least one attempt has recorded actual SCORM progress.
- `Completed` means one attempt reached the course completion rule.
- `Cancelled` means the enrollment was explicitly ended before completion.

Current cancellation rule:

- cancellation is manual/admin-driven in v1
- future automation may be added later without changing enrollment ownership

Important invariant:

```text
For one IdentityId + CourseId:
- at most one active enrollment
- active = Assigned or InProgress
- terminal = Completed or Cancelled
```

Repeated training rule:

- a repeated training cycle creates a new enrollment after the prior enrollment is terminal
- the system does not create concurrent active enrollments for the same learner and course

## Attempt

`Attempt` is one learner execution of SCORM content within an enrollment.

Recommended shape:

```text
Attempt
- Id
- EnrollmentId
- CourseId
- CourseVersionId
- IdentityId
- Status
- StartedAt
- LastActivityAt?
- CompletedAt?
- CompletionStatus?
- SuccessStatus?
- Score?
- ScoreScaled?
- IsScored
```

Recommended enum:

```text
AttemptStatus
- Active
- Completed
- Abandoned
```

Attempt rules:

- one enrollment may have many attempts before completion
- the first attempt that reaches the course completion rule completes the enrollment
- after an enrollment is `Completed`, no new attempt may be created for that enrollment
- a learner cannot relaunch a completed enrollment in v1; a new learning cycle requires a new enrollment
- an existing attempt stays on its original `CourseVersionId`
- a later attempt in the same still-active enrollment may use the newer current course version

Launch-without-progress rule:

- creating a launch session does not by itself move the enrollment to `InProgress`
- only actual persisted SCORM progress moves the enrollment from `Assigned` to `InProgress`
- if a launch never records progress, no `Attempt` is stored

## Launch Session

`LaunchSession` is short-lived runtime access for one attempt.

Recommended shape:

```text
LaunchSession
- Id
- AttemptId?
- EnrollmentId
- CourseId
- CourseVersionId
- ScoId
- IdentityId
- Token
- ExpiresAt
- CreatedAt
```

`LaunchSession` is operational state, not the learner history record.

Rules:

- session tokens are time-limited
- one course launch resolves through `CourseVersion` and SCO metadata
- session creation may precede persisted progress or even persisted attempt history
- if progress is later recorded, the runtime flow creates the `Attempt` and links the session to it

## ScormProgress

`ScormProgress` is the persisted normalized runtime state for an attempt.

Recommended shape:

```text
ScormProgress
- Id
- AttemptId
- CourseId
- CourseVersionId
- ScoId
- IdentityId
- ScormVersion
- CompletionStatus
- SuccessStatus
- Score?
- BookmarkLocation?
- SessionTime?
- SuspendData?
- RawCmiData
- LastCommittedAt
```

Persistence rules:

- store normalized fields needed for reporting and rollups
- preserve enough raw CMI payload to support resume fidelity
- progress persists on commit and terminate
- the latest saved progress is restored on relaunch for the same attempt when applicable

## Completion And Score Semantics

Completion is not a separate aggregate in v1.

Rules:

- completion is a terminal attempt outcome
- enrollment completion is rolled up from the first completing attempt
- `Enrollment.CompletedAttemptId` points to the winning attempt
- score is optional and stored only when the course/runtime emits one
- `CourseVersion.EmitsScore` indicates whether score is expected from that package/runtime
- `Learning` stores the score fact but does not decide whether a score is sufficient for requirement compliance

Recommended rollup behavior:

```text
Attempt completes course
-> persist final attempt outcome
-> set Enrollment.Status = Completed
-> set Enrollment.CompletedAt
-> set Enrollment.CompletedAttemptId
```

## SCORM Boundary

Runtime behavior follows the SCORM integration brief:

- support SCORM 1.2 and SCORM 2004
- detect runtime version from manifest metadata when possible
- parse `imsmanifest.xml` after package extraction
- expose the correct browser runtime API for the package version
- serve package files through token-based content access
- store progress through application APIs rather than direct package callbacks into persistence

Business boundary rules:

- business users interact with `Course` and `Enrollment`
- SCOs and CMI state are technical runtime details owned by `Learning`
- SCORM package structure must not leak into requirement ownership

## Future Requirement Integration

Manual enrollment comes first.

Later integration should follow this split:

- `Learning` owns course assignment and completion facts
- `Requirements` owns whether a completion counts as evidence, how long it stays valid, and whether a minimum score is required
- a saga or application service owns the cross-context rule that maps one or more courses to one or more requirements

Important operating rule:

- Fabric does not create learning enrollments automatically in the background just because a learning-fulfillable requirement is missing
- completion surfaces decide when to inspect relevant missing learning requirements and when to offer courses to the user

Recommended future flow:

```text
Requirement/course mapping configured
-> completion surface identifies relevant missing learning requirements for the current source context
-> completion surface resolves mapped courses
-> user selects course
-> application service upserts Enrollment in Learning
-> learner completes course attempt
-> Learning emits completion event
-> saga/application service writes RequirementEvidence in Requirements
-> Requirements reevaluates affected compliance
```

Completion surface examples:

- a visitor or contractor flow can inspect grants whose source is the current visit or job
- the surface can look at grant compliance results for that source
- if one or more missing requirements are learning-fulfillable, the surface can offer the mapped courses
- if the learner already has an active enrollment for that course, the flow reuses it rather than creating a duplicate assignment

Important boundary rule:

- `Learning` does not own evidence expiry, retraining cadence, or pass/fail policy for requirement satisfaction

## Open Questions Deferred

These are intentionally deferred, not unresolved blockers for the current design:

- whether future requirement-driven enrollments need additional source metadata on `Enrollment`
- whether no-progress launches need explicit operational telemetry outside the core domain model
- whether future non-SCORM learning sources should reuse `Course` and `Enrollment` with a pluggable content/runtime type model
