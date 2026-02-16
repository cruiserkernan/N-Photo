# ARCH-002: Test Project Topology

- Status: Implemented
- Last Updated: 2026-02-16
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
- `tests/App.Ui.Tests.Common` (class library, no test runner)
  - shared UI test helpers (artifact paths, wait helpers, screenshot validation, desktop gate checks)
- `tests/App.Ui.Headless.Tests`
  - deterministic headless Avalonia UI automation + screenshot scenarios (default test flow)
- `tests/App.Ui.Desktop.Tests`
  - Windows desktop UIA automation via FlaUI (opt-in with environment gate)
  - targets `net10.0-windows` to align with UI Automation dependencies

## Boundaries
- Unit test projects should reference only the production project under test plus `Editor.Tests.Common`.
- `Editor.Integration.Tests` may reference multiple production projects intentionally.
- Shared helpers in `Editor.Tests.Common` must avoid dependencies on Avalonia UI.
- `App.Ui.Headless.Tests` may reference `src/App` to instantiate full shell UI in a headless runtime.
- `App.Ui.Desktop.Tests` should treat the app as an external process boundary and avoid direct in-process UI coupling.
- Desktop UI tests are opt-in and gated by `NPHOTO_ENABLE_DESKTOP_UI_TESTS=1`; they must skip instead of fail when gate prerequisites are not met.

## Conventions
- Keep namespace aligned with project names.
- Keep one concern per test project.
- Prefer deterministic fixtures over external assets for CI reliability.

## Open Questions
- None.
