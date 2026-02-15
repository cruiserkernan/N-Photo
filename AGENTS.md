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

## Bug Fix Workflow (Exception)
For bug fixes, do not use spec-driven files. Fix bugs directly in code.
- No architecture spec or feature spec is required for bug-only work.
- Implement the smallest safe fix, then run relevant checks/tests.
- Keep changes focused on correcting the bug behavior.
- Update previous specs only if the bug revealed a spec gap or error that needs correction.

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

## Checks and Tests
- Use automated tests where possible to validate behavior against specs.
- Include unit tests for core logic and integration tests for end-to-end behavior.
- Perform manual testing for UI/UX features or complex interactions that are difficult to automate.
- Run app for a short period after implementation to catch any runtime issues or regressions before marking as implemented. Make sure to quit after a few seconds so that AI does not get stuck infinitely.

## Spec Rules
- Keep specs small and testable.
- Separate architecture decisions from feature behavior.
- Every feature spec must link to at least one architecture spec.
- Include acceptance criteria in every spec.
- Track open questions explicitly; do not hide assumptions in code.
- If questions are unresolved, stay in planning mode and do not implement.
- Implementation must trace back to a specific approved feature spec.

## Refactoring Guidance
- For every feature, proactively identify refactors that improve extensibility, maintainability, or architecture quality.
- Apply refactors only when they are directly within the feature scope and support the approved behavior.
- Make sure to always think of extensibility and future-proofing.
- Prefer refactors that reduce current complexity, duplication, or coupling in code being touched by the feature.
- If a larger cross-cutting overhaul would be valuable but is out of scope, document and propose it explicitly instead of implementing it implicitly.

## Status Values
- `Draft`
- `Approved`
- `Implemented`
- `Deprecated`

## Naming
- Architecture specs: `ARCH-###-short-name.md`
- Feature specs: `FEAT-###-short-name.md`

## 3rd Party Libraries
- Use only well-maintained, widely adopted libraries with permissive licenses.
- Document any new dependencies in the architecture spec and justify their use.
- Avoid adding dependencies for trivial functionality that can be implemented in-house without significant effort or maintenance burden.
- Regularly review and update dependencies to ensure security and compatibility.
