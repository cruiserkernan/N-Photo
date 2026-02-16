# ARCH-000: Current System Architecture (Living)

- Status: Implemented
- Last Updated: 2026-02-15
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
- `Editor.IO`: import/export adapters.

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

## UI Composition
- `MainWindow` is split into partial files by responsibility:
  - shell/orchestration
  - graph canvas behavior
  - viewer viewport behavior
- Graph canvas uses world-space pan/zoom with connection-drag wire routing.
- Graph canvas directed topology:
  - standard inputs on top edge
  - outputs on bottom edge
  - mask inputs on right edge
- Graph canvas wire rendering is center-routed and clipped to dynamic node border intersections; connected edges show arrowheads on destination inputs only, while unconnected input/output stubs keep arrowheads for affordance.
- Viewer panel supports pan/zoom navigation with fit-to-image initialization.

## Maintenance Rules
- Keep this doc current after each implemented feature or refactor that changes architecture.
- If architecture grows substantially, add additional living architecture docs and cross-link them here.
- Do not treat architecture as one-file-per-feature by default; prefer updating living docs.

## Verification
- `dotnet build`
- `dotnet test`
- short app launch smoke test (`dotnet run --project src/App/App.csproj`, then exit)
