# AGENTS

This repo follows a spec-driven workflow for building a node-based photo editor in C# with Avalonia.

## Feature Workflow (Required)
Use this flow for every feature request:
1. Plan the feature first.
2. Ask the user any blocking questions.
3. Wait for explicit user approval of the plan.
4. Implement only after approval.
5. Validate against acceptance criteria.
6. Report results and update spec status.

## Detailed Steps
1. Create or update the relevant architecture spec in `specs/architecture/` if needed.
2. Create or update the feature spec in `specs/features/`.
3. Produce an implementation plan from the spec:
   - scope
   - files/modules to change
   - tests to add/update
   - risks and assumptions
4. Ask concise clarification questions for any unknowns that affect design or behavior.
5. Do not start coding until the user says the plan is approved.
6. Implement in small, reviewable changes.
7. Run checks/tests and compare outcome to spec acceptance criteria.
8. Mark status:
   - `Draft` while planning
   - `Approved` after user approval
   - `Implemented` after code + verification are complete
   - `Deprecated` when replaced or retired

## Spec Rules
- Keep specs small and testable.
- Separate architecture decisions from feature behavior.
- Every feature spec must link to at least one architecture spec.
- Include acceptance criteria in every spec.
- Track open questions explicitly; do not hide assumptions in code.
- If questions are unresolved, stay in planning mode and do not implement.
- Implementation must trace back to a specific approved feature spec.

## Status Values
- `Draft`
- `Approved`
- `Implemented`
- `Deprecated`

## Naming
- Architecture specs: `ARCH-###-short-name.md`
- Feature specs: `FEAT-###-short-name.md`
