# FEAT-001: MVP Node Set

- Status: Draft
- Last Updated: 2026-02-14
- Linked Architecture Specs: `specs/architecture/ARCH-001-mvp-foundation.md`

## Problem
We need a minimal, end-to-end node set to validate graph editing, rendering, caching, and export.

## Scope
Include only enough nodes to prove the architecture and workflow.

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
- Editor must prevent cyclic graph connections.

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
- First release parameter ranges and defaults per node.
- Which blur algorithm to standardize for golden tests.

