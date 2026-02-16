# ARCH-000: Current System Architecture (Living)

- Status: Implemented
- Last Updated: 2026-02-16
- Scope: Canonical, continuously updated architecture baseline for N-Photo

## Purpose
- This is a living architecture document.
- Update this file whenever core design, boundaries, contracts, or technology decisions change.
- Feature specs should reference this document unless another living architecture doc is more specific.

## System Boundaries
- `App`: Avalonia shell composition and workspace orchestration.
- `App.Presentation`: UI interaction controllers and property editor primitive system.
- `Editor.Application`: app-facing session boundary (`IEditorSession`).
- `Editor.Engine`: graph execution, scheduling, caching, command handling.
- `Editor.Domain`: graph and image domain contracts.
- `Editor.Imaging`: node kernels for image operations.
- `Editor.IO`: image import/export plus project document serialization (`.nphoto`).

## Core Decisions
- Runtime image pipeline is half-float (`16-bit float`) in `Editor.Domain.Imaging.RgbaImage`.
- Render kernel operations run in the internal float pipeline without per-node RGBA8 roundtrips.
- Preview/export remain RGBA8-compatible outputs at the app boundary.
- Node port metadata is explicit in domain contracts (`NodePortDefinition.Role`) so UI placement and behavior can be data-driven.
- Mask-capable processing nodes expose a dedicated `Mask` input port and runtime evaluation applies mask-weighted blending between pre/post operation results.
- Node properties UI is built from reusable editor primitives:
  - slider
  - number-input
  - enum-select
  - toggle
  - color
- Nodes can select property UI presentation through `NodeParameterDefinition.EditorPrimitive`.
- Graph and viewer both use shared pan/zoom math via `PanZoomController`.
- Graph wire geometry math is centralized in `GraphWireGeometryController` (arrowheads, line-grab segments, segment distance, tip offsets).
- Blend mode handling supports typed `BlendMode` with compatibility parsing for legacy string modes.
- Session-level graph persistence is explicit through `GraphDocumentState` and `GraphNodeState`, exposed by `IEditorSession` and `IEditorEngine` via capture/load methods.
- Project documents use versioned JSON (`.nphoto`, `formatVersion = 1`) with deterministic ordering for stable signatures and dirty-state comparison.
- Project asset bindings are stored as relative paths when they resolve inside project directory boundaries, with absolute fallback for external assets.
- Dirty tracking compares canonical project signatures rather than mutation counters so undo/redo can clear dirty state when content matches last save.
- Test-only UI automation is enabled through environment variables (`NPHOTO_AUTOMATION_MODE`, `NPHOTO_AUTOMATION_SCENARIO`, `NPHOTO_AUTOMATION_SCREENSHOT_PATH`) and remains default-off in normal app usage.
- UI automation supports deterministic startup scenarios (`startup-shell`, `startup-add-transform`) and writes screenshot artifacts to `artifacts/ui-screenshots/`.
- App shell controls expose stable `AutomationId` selectors for UI automation across headless and desktop test suites.

## UI Composition
- `MainWindow` is split into partial files by responsibility:
  - composition/dependency wiring
  - lifecycle/event binding
  - toolbar commands and node-add/search
  - keyboard preview routing
  - selection/status/properties orchestration
  - graph canvas state
  - graph node-card/port composition
  - graph input handling
  - graph connection-drag behavior
  - graph viewport utilities
  - viewer viewport behavior
- Graph canvas uses world-space pan/zoom with connection-drag wire routing.
- Graph canvas directed topology:
  - standard inputs on top edge
  - outputs on bottom edge
  - mask inputs on right edge
- Graph canvas wire rendering is center-routed and clipped to dynamic node border intersections; connected edges show arrowheads on destination inputs only, while unconnected input/output stubs keep arrowheads for affordance.
- Graph canvas connection drop targeting resolves by precise port snap first, then node-body fallback targeting (full node body + padding) with default compatible ports.
- Graph canvas node-body interaction takes precedence over edge/stub grabs; inside-node pointer-down always initiates node drag.
- Graph canvas supports wire splice workflows:
  - drop a node onto a wire to auto-insert via node primary input/output
  - double-click a wire to insert and splice an elbow node.
- Connection retargeting to empty/incompatible space commits disconnection when a source edge was detached; `Esc` cancels active wire drag without mutation.
- Session/runtime graph editing now includes explicit node removal and node bypass operations with undo/redo support and protected-node guards for canonical input/output nodes.
- Viewer panel supports pan/zoom navigation with fit-to-image initialization.
- Shell file cluster supports `New`, `Open`, `Save`, `Save As`, and `Export`.
- Unsaved-change prompt flow is integrated for `New`, `Open`, and window close, with `Save`/`Discard`/`Cancel` decisions.
- Persisted UI state includes node positions, selected node, preview slot assignments, and active preview slot; docking layout and viewport pan/zoom remain out of scope.
- App visual resources/styles are organized through merged dictionaries under `src/App/Styles/`.
- Dialog-based picker flows are suppressed during automation mode so tests avoid modal blockers.

## Maintenance Rules
- Keep this doc current after each implemented feature or refactor that changes architecture.
- If architecture grows substantially, add additional living architecture docs and cross-link them here.
- Do not treat architecture as one-file-per-feature by default; prefer updating living docs.

## Verification
- `dotnet build`
- `dotnet test`
- short app launch smoke test (`dotnet run --project src/App/App.csproj`, then exit)
- headless UI automation: `dotnet test tests/App.Ui.Headless.Tests/App.Ui.Headless.Tests.csproj -c Debug`
- desktop UI automation (opt-in): `$env:NPHOTO_ENABLE_DESKTOP_UI_TESTS='1'; dotnet test tests/App.Ui.Desktop.Tests/App.Ui.Desktop.Tests.csproj -c Debug`
