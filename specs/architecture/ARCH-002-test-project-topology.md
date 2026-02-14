# ARCH-002: Test Project Topology

- Status: Implemented
- Last Updated: 2026-02-14
- Scope: Test-solution structure for unit, integration, and shared test infrastructure

## Goals
- Reduce coupling by keeping tests close to the module they validate.
- Keep integration coverage explicit and separate from module unit tests.
- Avoid duplicated helper code via a shared test utility project.

## Project Topology
- `tests/Editor.Tests.Common` (class library, no test runner)
  - shared fixtures and helpers (temp directories, deterministic test image builders, file assertions)
- `tests/Editor.Domain.Tests`
  - graph model and DAG validation tests
- `tests/Editor.Engine.Tests`
  - command workflow, undo/redo, render orchestration tests
- `tests/Editor.IO.Tests`
  - image load/export behavior (format support, failure handling)
- `tests/Editor.Imaging.Tests`
  - node-kernel behavior tests (determinism and core operation correctness)
- `tests/Editor.Integration.Tests`
  - end-to-end non-UI flow across IO + engine + imaging

## Boundaries
- Unit test projects should reference only the production project under test plus `Editor.Tests.Common`.
- `Editor.Integration.Tests` may reference multiple production projects intentionally.
- Shared helpers in `Editor.Tests.Common` must avoid dependencies on Avalonia UI.

## Conventions
- Keep namespace aligned with project names.
- Keep one concern per test project.
- Prefer deterministic fixtures over external assets for CI reliability.

## Open Questions
- None.
