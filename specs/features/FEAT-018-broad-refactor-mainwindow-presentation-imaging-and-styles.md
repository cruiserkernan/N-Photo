# FEAT-018: Broad Refactor Wave for MainWindow, Presentation Primitives, Imaging Kernels, and Styles

- Status: Implemented
- Last Updated: 2026-02-16
- Linked Architecture Specs:
  - `specs/architecture/ARCH-000-current-system-architecture.md`
  - `specs/architecture/ARCH-012-shell-redesign-and-presentation-architecture.md`
- Status History:
  - `Draft` (2026-02-16)
  - `Approved` (2026-02-16)
  - `Implemented` (2026-02-16)

## Problem
The app had several oversized files carrying mixed responsibilities:
- graph canvas state, rendering, input, geometry, and viewport code in one file
- shell composition, lifecycle, commands, keyboard routing, and status logic in one file
- parameter primitive registry plus all primitive implementations in one file
- imaging kernels and helpers in one monolithic static class
- app resources and styles in one large `App.axaml`

This increased refactor risk and slowed future feature work.

## Scope
- Split `MainWindow` graph canvas and shell code-behind into focused partial files.
- Extract reusable wire-geometry helpers into presentation controller layer.
- Split parameter primitive implementations into per-primitive files.
- Split imaging kernels into concern-focused partial class files.
- Introduce typed blend mode API with compatibility string overload.
- Split app resources/styles into dedicated style dictionary files.

## Requirements
- Preserve existing behavior for graph interaction and viewer interaction.
- Keep compatibility for existing blend-mode string usage.
- Keep style behavior unchanged after dictionary split.
- Keep all file moves traceable and build/test validated.

## Acceptance Criteria
- No single `MainWindow*.cs` file exceeds 500 lines.
- Graph wire geometry helpers exist outside `MainWindow`.
- `ParameterEditorPrimitiveRegistry` is registry/resolution-only.
- Imaging kernels are split into operation-focused partial files.
- Typed `BlendMode` support exists with compatibility string overload.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- Short app smoke run succeeds (`dotnet run --project src/App/App.csproj -c Debug --no-build`, then exit).

## Implementation Summary
- Graph canvas split into:
  - `MainWindow.GraphCanvas.State.cs`
  - `MainWindow.GraphCanvas.NodeCards.cs`
  - `MainWindow.GraphCanvas.Input.cs`
  - `MainWindow.GraphCanvas.ConnectionDrag.cs`
  - `MainWindow.GraphCanvas.Viewport.cs`
- New wire geometry controller:
  - `src/App.Presentation/Controllers/GraphWireGeometryController.cs`
- Shell code-behind split into:
  - `MainWindow.Composition.cs`
  - `MainWindow.Lifecycle.cs`
  - `MainWindow.ToolbarCommands.cs`
  - `MainWindow.KeyboardRouting.cs`
  - `MainWindow.SelectionAndStatus.cs`
- Parameter primitives moved to:
  - `src/App.Presentation/Controllers/ParameterEditors/*.cs`
- Imaging kernels split into:
  - `MvpNodeKernels.Common.cs`
  - `MvpNodeKernels.Geometry.cs`
  - `MvpNodeKernels.Color.cs`
  - `MvpNodeKernels.Convolution.cs`
  - `MvpNodeKernels.Blend.cs`
- Typed blend mode support:
  - `src/Editor.Domain/Imaging/BlendMode.cs`
  - `src/Editor.Imaging/BlendModeParser.cs`
  - `BlendNodeModule` updated to typed mode path
- App styling split into:
  - `src/App/Styles/Brushes.axaml`
  - `src/App/Styles/BaseControls.axaml`
  - `src/App/Styles/ShellPanels.axaml`
  - `src/App/Styles/GraphAndViewer.axaml`
  - `src/App/App.axaml` updated to include dictionaries

## Test Updates
- Added `tests/App.Presentation.Tests/GraphWireGeometryControllerTests.cs`
- Expanded `tests/App.Presentation.Tests/ParameterEditorPrimitiveRegistryTests.cs`
- Expanded `tests/Editor.Imaging.Tests/MvpNodeKernelsTests.cs`

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
- short app smoke run completed (`dotnet run --project src/App/App.csproj -c Debug --no-build`, terminated after a short launch window).

## Out of Scope
- Runtime/engine behavior redesign.
- New node types or new graph validity semantics.
- Visual redesign beyond style file reorganization.
