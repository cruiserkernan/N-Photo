# FEAT-002: Repository Gitignore Baseline

- Status: Implemented
- Last Updated: 2026-02-14
- Linked Architecture Specs: `specs/architecture/ARCH-001-mvp-foundation.md`
- Depends On: `specs/features/FEAT-000-project-bootstrap.md`

## Problem
The repository currently has no root `.gitignore`, which allows generated build output and IDE-local files to be committed.

## Scope
Add a baseline `.gitignore` for this .NET + Avalonia solution so generated artifacts stay out of version control.

## Requirements
- Add a root `.gitignore`.
- Ignore build and restore outputs produced by SDK-style .NET projects:
  - `bin/`
  - `obj/`
- Ignore common IDE and local user artifacts:
  - `.vs/`
  - `.vscode/`
  - `.idea/`
  - `*.user`
  - `*.suo`
- Ignore common OS-local artifacts:
  - `.DS_Store`
  - `Thumbs.db`
- Do not ignore source code, specs, or solution/project definitions.

## Acceptance Criteria
- A new file exists at repository root: `.gitignore`.
- In a clean clone, after `dotnet build`, generated build outputs are excluded from `git status` by ignore rules.
- In a clean clone, common local IDE metadata files listed above are excluded from `git status`.

## Out of Scope
- Rewriting repository history.
- Broad cleanup of already tracked generated files unless explicitly approved.

## Open Questions
- None.

## Decisions
- 2026-02-14: Implement FEAT-002 as `ignore-only` (no untracking of already committed artifacts in this feature).

## Validation
- `Test-Path .gitignore` returns `True`.
- `git check-ignore -v --no-index src/App/bin/Debug/net10.0/App.dll src/App/obj/Debug/net10.0/App.dll .vs/settings.vssettings .vscode/settings.json .idea/workspace.xml local.user local.suo .DS_Store Thumbs.db` shows matches for all required ignore rules.
