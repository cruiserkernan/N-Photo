# FEAT-000: Project Bootstrap

- Status: Implemented
- Last Updated: 2026-02-14
- Linked Architecture Specs: `specs/architecture/ARCH-001-mvp-foundation.md`

## Problem
We need a runnable and testable solution structure before implementing node behavior.

## Scope
Create the minimal project foundation and wiring required for subsequent feature work.

## Requirements
- Create solution and projects aligned with architecture:
  - `src/App`
  - `src/Editor.Domain`
  - `src/Editor.Engine`
  - `src/Editor.Imaging`
  - `src/Editor.IO`
  - `tests/Editor.Tests`
- Enforce reference boundaries:
  - `Editor.Domain` has no Avalonia/Skia dependencies.
  - `App` references engine/domain/io via public interfaces.
- App starts to a basic shell window with placeholders for:
  - node graph area
  - property panel
  - viewport area
  - toolbar actions for load/export (can be stubbed)
- Add CI-friendly smoke checks:
  - solution restore/build
  - test project execution
- Include minimal domain graph primitives:
  - node/edge model
  - DAG validation API (cycle rejection)

## Acceptance Criteria
- `dotnet build` succeeds for the full solution.
- `dotnet test` runs and passes at least one smoke test.
- Launching `App` opens the main shell window without runtime exceptions.
- Domain DAG validation rejects cyclic connections in tests.

## Out of Scope
- Full node kernel implementations.
- Rendering pipeline and tile cache behavior.
- Final UX polish of node graph editor.

## Decisions
- Target .NET SDK baseline: `.NET 10`.
- Avalonia baseline on `.NET 10`: latest stable (`11.3.12` at implementation time).
- Analyzer enforcement in MVP: enabled with .NET analyzers and `AnalysisLevel=latest`.

## Open Questions
- None.

## Validation
- `dotnet restore NPhoto.slnx` succeeds.
- `dotnet build NPhoto.slnx -c Debug --no-restore` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` passes (3 tests).
- App startup smoke check passes (process starts and remains alive).
