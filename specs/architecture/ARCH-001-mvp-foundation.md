# ARCH-001: MVP Foundation

- Status: Draft
- Last Updated: 2026-02-14
- Scope: Minimal architecture for a node-based photo editor (Avalonia + C#)

## Goals
- Define clear assembly boundaries.
- Keep domain model independent from UI and imaging backends.
- Enable deterministic graph evaluation with tile cache and cancellation.

## Solution Layout
- `App` (Avalonia UI)
- `Editor.Domain` (pure models)
- `Editor.Engine` (graph evaluation, tiling, cache)
- `Editor.Imaging` (image operation kernels, CPU/Skia-backed for MVP)
- `Editor.IO` (load/save, serialization, assets)
- `Editor.Tests` (golden images, determinism, cache invalidation)

## Core Domain Concepts
- `Document`: graph, node state, viewport state, asset refs.
- `Graph`: nodes + edges; must be a DAG.
- `Node`: id, type, ports, parameter schema.
- `ParameterValue`: typed union (`float`, `int`, `bool`, `string`, `Color`, `Curve`, `Enum`, ...).

Constraint: `Editor.Domain` must not depend on Avalonia or Skia types.

## Engine Concepts
- `IImage`: immutable image abstraction.
- `TileKey`: `(nodeId, level, x, y, quality)`.
- `IRenderBackend`: `Evaluate(...)`, `EvaluateTile(...)`.
- `GraphCompiler`: validates DAG, produces topological plan, stable fingerprints.
- `Scheduler`: cancellation, rapid-change coalescing, latest request wins.

## Imaging Concepts
- `INodeKernel`: per-node implementation unit.
- Kernel metadata: halo pixels, output size, deterministic capability.
- `TileContext`: requested rect, quality, input tile accessor, parameters.

MVP backend: CPU/Skia in `Editor.Imaging` only.

## Cache
- LRU `TileCache`, configurable memory budget.
- Key includes node id, upstream hash, param hash, tile coordinates, mip, quality.
- `NodeFingerprint` must be deterministic and versioned.

## IO
- `IAssetStore`: asset id to path/stream.
- `DocumentSerializer`: JSON for graph + params + asset refs.
- `ImageLoader`: PNG/JPEG/TIFF (start minimal).
- `ImageExporter`: final render to file.

## UI Integration
- MVVM: `MainWindow`, `NodeGraphView`, `PropertyPanel`, `ViewportView`, `Toolbar`.
- Command system with undo/redo via `IEditorCommand`.
- Viewport renders preview during interaction, final-quality after idle.

## MVP Evaluation Flow
1. UI issues `SetParam`.
2. Document updates, affected subgraph invalidated.
3. Scheduler receives render request.
4. Engine serves cached tiles or evaluates recursively.
5. UI draws arriving tiles and cancels stale renders.

## Open Questions
- Initial tile size and mip strategy.
- Preview-to-final quality switch timing.
- Exact serialization versioning strategy.

