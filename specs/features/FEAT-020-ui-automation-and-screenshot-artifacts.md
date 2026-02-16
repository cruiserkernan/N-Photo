# FEAT-020: Agent-Driven UI Automation and Screenshot Artifacts

- Status: Implemented
- Last Updated: 2026-02-16
- Linked Architecture Specs:
  - `specs/architecture/ARCH-000-current-system-architecture.md`
  - `specs/architecture/ARCH-002-test-project-topology.md`
- Status History:
  - `Draft` (2026-02-16)
  - `Approved` (2026-02-16)
  - `Implemented` (2026-02-16)

## Problem
Agents and developers could run domain/presentation tests, but there was no deterministic UI automation harness to validate shell behavior through UI scenarios and collect screenshot artifacts while implementing changes.

## Scope
- Add an app-level automation seam driven by environment variables (default off).
- Add stable `AutomationId` selectors for key shell controls.
- Add shared UI test utilities for artifact paths, PNG validation, waits, and desktop test gating.
- Add headless UI automation tests for deterministic screenshot scenarios.
- Add opt-in desktop FlaUI automation tests for real-window checks and screenshots.
- Document commands and artifact paths.

## Requirements
- Automation seam must be inert unless `NPHOTO_AUTOMATION_MODE=1`.
- Supported automation scenarios (v1):
  - `startup-shell`
  - `startup-add-transform`
- App automation must honor:
  - `NPHOTO_AUTOMATION_MODE`
  - `NPHOTO_AUTOMATION_SCENARIO`
  - `NPHOTO_AUTOMATION_SCREENSHOT_PATH`
- Desktop suite must be opt-in via `NPHOTO_ENABLE_DESKTOP_UI_TESTS=1` and skip otherwise.
- Screenshot artifacts must be generated on every UI scenario run to deterministic paths under `artifacts/ui-screenshots/`.

## Acceptance Criteria
- Headless startup shell scenario writes `artifacts/ui-screenshots/headless/startup-shell.png`.
- Headless startup add-transform scenario writes `artifacts/ui-screenshots/headless/startup-add-transform.png`.
- Headless add-transform scenario status text indicates node addition.
- Desktop add-transform scenario writes `artifacts/ui-screenshots/desktop/startup-add-transform.png`.
- Desktop tests skip (not fail) when opt-in gate is not enabled.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- `dotnet test tests/App.Ui.Headless.Tests/App.Ui.Headless.Tests.csproj -c Debug` succeeds.
- Desktop command is available and documented:
  - `$env:NPHOTO_ENABLE_DESKTOP_UI_TESTS='1'; dotnet test tests/App.Ui.Desktop.Tests/App.Ui.Desktop.Tests.csproj -c Debug`

## Implementation Summary
- Added app automation seam:
  - `src/App/Automation/AppAutomationOptions.cs`
  - `src/App/Automation/AppAutomationScenarioRunner.cs`
  - `src/App/MainWindow.Automation.cs`
- Wired lifecycle to run automation scenarios when window opens and cancel on close.
- Suppressed modal picker/dialog workflows in automation mode to avoid blocking UI tests.
- Added stable `AutomationProperties.AutomationId` values in shell and panel XAML files.
- Added UI test utility project:
  - `tests/App.Ui.Tests.Common/`
- Added headless UI suite:
  - `tests/App.Ui.Headless.Tests/`
- Added desktop FlaUI suite:
  - `tests/App.Ui.Desktop.Tests/`
  - desktop suite targets `net10.0-windows` for UIA compatibility
- Added UI automation usage docs:
  - `tests/UI-AUTOMATION.md`
- Added solution wiring and ignore rules for test artifacts:
  - `NPhoto.slnx`
  - `.gitignore`

## Test Updates
- `tests/App.Ui.Headless.Tests/MainWindowAutomationHeadlessTests.cs`
- `tests/App.Ui.Desktop.Tests/MainWindowAutomationDesktopTests.cs`

## Validation
- `dotnet build NPhoto.slnx -c Debug`
- `dotnet test NPhoto.slnx -c Debug --no-build`
- `dotnet test tests/App.Ui.Headless.Tests/App.Ui.Headless.Tests.csproj -c Debug`
- `dotnet test tests/App.Ui.Desktop.Tests/App.Ui.Desktop.Tests.csproj -c Debug` (desktop suite skipped by default without opt-in)
- `$env:NPHOTO_ENABLE_DESKTOP_UI_TESTS='1'; dotnet test tests/App.Ui.Desktop.Tests/App.Ui.Desktop.Tests.csproj -c Debug --no-build`

## Out of Scope
- Pixel-perfect/golden baseline visual regression.
- Desktop UI tests as default CI gate.
- Automation coverage for file picker flows.
