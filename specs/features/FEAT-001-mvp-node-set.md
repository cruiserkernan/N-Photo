# FEAT-001: MVP Node Set

- Status: Implemented
- Last Updated: 2026-02-14
- Linked Architecture Specs: `specs/architecture/ARCH-001-mvp-foundation.md`
- Depends On: `specs/features/FEAT-000-project-bootstrap.md`

## Problem
We need a minimal, end-to-end node set to validate graph editing, rendering, caching, and export.

## Scope
Implement MVP node behavior and integrate it into the bootstrap structure from FEAT-000.

## Requirements
- Support these node types:
  - `ImageInput`
  - `Transform` (scale/rotate)
  - `Crop`
  - `ExposureContrast`
  - `Curves` (simple)
  - `HSL`
  - `Blur` (box or gaussian)
  - `Sharpen`
  - `Blend` (`over`, `multiply`, `screen`)
  - `Output`
- Each node must define:
  - typed parameters with defaults
  - input/output ports
  - deterministic execution behavior for same inputs/params
- Integrate with DAG validation and command workflow provided by FEAT-000.

## Acceptance Criteria
- A user can load an image, chain at least 4 processing nodes, and preview output.
- Parameter changes update visible tiles without blocking UI interaction.
- Undo/redo works for add node, connect ports, and set parameter.
- Export writes the final output image to disk.

## Out of Scope (MVP)
- GPU kernels
- Layer masks
- Batch processing
- Distributed rendering

## Open Questions
- None.

## Decisions
- Blur algorithm for MVP tests: `Gaussian`.
- MVP graph editing UX: simple list/select/connect controls (not full drag-and-drop canvas).
- Imaging backend for MVP node execution: `SkiaSharp`.
- Load/export formats for MVP: `PNG` and `JPEG`.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` passes (`5` tests).
- App shell now provides:
  - image load (`PNG`/`JPEG`)
  - simple node add/connect workflow
  - parameter editing
  - undo/redo for add/connect/set parameter
  - live preview updates
  - export (`PNG`/`JPEG`)
