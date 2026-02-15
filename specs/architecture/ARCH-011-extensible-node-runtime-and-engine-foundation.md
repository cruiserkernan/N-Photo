# ARCH-011: Extensible Node Runtime and Engine Foundation

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: Modular node runtime, registry-driven evaluation, and foundational engine seams for compiler/scheduler/cache/backend extensibility

## Goals
- Replace switch-based node evaluation dispatch with a class-based node module registry.
- Introduce explicit engine abstraction seams for graph compilation, scheduling, caching, and backend execution.
- Keep node and engine contracts extensible without changing app-facing workflows.

## Decisions
- Added `Editor.Engine.Abstractions` for shared contracts:
  - `NodeTypeId`, `NodeTypeDescriptor`
  - `INodeModule`, `INodeModuleRegistry`, `INodeEvaluationContext`
  - `IGraphCompiler`, `GraphExecutionPlan`
  - `IRenderScheduler`, `ITileCache`, `IRenderBackend`, `TileKey`, `NodeFingerprint`
  - moved `IEditorEngine` and `PreviewFrame` to abstractions assembly.
- Added `Editor.Nodes` with compile-time module registration via `BuiltInNodeModuleRegistry`.
- Added one module class per built-in node type under `src/Editor.Nodes/Modules/`.
- Reworked `BootstrapEditorEngine` to orchestrate focused collaborators:
  - command processor (`EditorCommandProcessor`)
  - input-image store (`InputImageStore`)
  - graph compiler (`Rendering/GraphCompiler`)
  - scheduler (`Rendering/LatestRenderScheduler`)
  - cache (`Rendering/InMemoryTileCache`)
  - backend (`Rendering/SkiaRenderBackend`)
- Kept graph/domain validation semantics intact (DAG + port checks) while routing node execution through modules.

## Boundaries
- `Editor.Domain` remains free of Avalonia/Skia dependencies.
- `Editor.Nodes` owns built-in node runtime modules.
- `Editor.Engine` owns orchestration and runtime flow.
- `Editor.Engine.Abstractions` owns reusable contracts.

## Refactoring Guidance
- Add new nodes by implementing `INodeModule` and registering in `BuiltInNodeModuleRegistry`.
- Keep engine orchestration free of node-type `switch` statements.
- Prefer extending abstractions over adding direct app-to-engine coupling.

## Open Questions
- Tile coordinates/quality strategy is currently placeholder-level (single preview tile path).
- Runtime plugin loading remains deferred; compile-time registration is the current model.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
