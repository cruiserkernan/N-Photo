# FEAT-003: Test Suite Restructure

- Status: Implemented
- Last Updated: 2026-02-14
- Linked Architecture Specs:
  - `specs/architecture/ARCH-001-mvp-foundation.md`
  - `specs/architecture/ARCH-002-test-project-topology.md`
- Depends On:
  - `specs/features/FEAT-001-mvp-node-set.md`

## Problem
All tests currently live in a single `Editor.Tests` project, which mixes concerns and makes module ownership, dependencies, and integration scope unclear.

## Scope
Split tests into module-focused projects, add shared test infrastructure, and add one explicit integration test for load -> process -> export workflow.

## Requirements
- Replace single `tests/Editor.Tests` with:
  - `tests/Editor.Tests.Common`
  - `tests/Editor.Domain.Tests`
  - `tests/Editor.Engine.Tests`
  - `tests/Editor.IO.Tests`
  - `tests/Editor.Imaging.Tests`
  - `tests/Editor.Integration.Tests`
- Update `NPhoto.slnx` to include all new test projects.
- Move existing tests into the appropriate projects.
- Add shared helpers in `Editor.Tests.Common` and consume from test projects.
- Add at least one end-to-end integration test that verifies:
  - load image from disk (PNG or JPEG)
  - apply multi-node processing chain via engine
  - export final image to disk
  - output file exists and is readable
- Ensure full build and tests pass in CI-friendly commands.

## Acceptance Criteria
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Test projects are split by concern as listed in requirements.
- Shared helper code exists only in `Editor.Tests.Common` (no duplicated temp-dir/image helper classes across test projects).
- Integration test executes end-to-end pipeline and passes.

## Out of Scope
- UI automation tests for Avalonia interactions.
- Snapshot/golden image baseline management.

## Open Questions
- None.

## Decisions
- Legacy `tests/Editor.Tests` project will be removed entirely (no compatibility wrapper).

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Test projects split by concern:
  - `tests/Editor.Domain.Tests`
  - `tests/Editor.Engine.Tests`
  - `tests/Editor.IO.Tests`
  - `tests/Editor.Imaging.Tests`
  - `tests/Editor.Integration.Tests`
  - shared utilities in `tests/Editor.Tests.Common`
- End-to-end integration test added:
  - `tests/Editor.Integration.Tests/LoadProcessExportIntegrationTests.cs`
