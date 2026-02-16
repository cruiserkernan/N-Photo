# UI Automation and Screenshot Artifacts

This repository provides two UI automation suites for agent-assisted validation:

- `tests/App.Ui.Headless.Tests` (default, deterministic, fast)
- `tests/App.Ui.Desktop.Tests` (Windows desktop UIA, opt-in)

Screenshot artifacts are always written to:

- `artifacts/ui-screenshots/headless/*.png`
- `artifacts/ui-screenshots/desktop/*.png`

## Automation Environment Contract

- `NPHOTO_AUTOMATION_MODE=1`
- `NPHOTO_AUTOMATION_SCENARIO=<scenario-id>`
- `NPHOTO_AUTOMATION_SCREENSHOT_PATH=<absolute-path>`
- `NPHOTO_ENABLE_DESKTOP_UI_TESTS=1` (enables desktop suite)

## Supported Scenarios

- `startup-shell`
- `startup-add-transform`

## Commands

1. Build all projects:

```powershell
dotnet build NPhoto.slnx -c Debug
```

2. Run full test suite (desktop tests are skipped unless explicitly enabled):

```powershell
dotnet test NPhoto.slnx -c Debug --no-build
```

3. Run headless UI suite only:

```powershell
dotnet test tests/App.Ui.Headless.Tests/App.Ui.Headless.Tests.csproj -c Debug
```

4. Run desktop UI suite only (Windows opt-in):

```powershell
$env:NPHOTO_ENABLE_DESKTOP_UI_TESTS='1'
dotnet test tests/App.Ui.Desktop.Tests/App.Ui.Desktop.Tests.csproj -c Debug
```
