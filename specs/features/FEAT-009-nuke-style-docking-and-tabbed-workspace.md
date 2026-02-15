# FEAT-009: Nuke-Style Docking and Tabbed Workspace

- Status: Implemented
- Last Updated: 2026-02-15
- Linked Architecture Specs:
  - `specs/architecture/ARCH-005-node-graph-worldspace-navigation.md`
  - `specs/architecture/ARCH-007-nuke-style-docking-workspace.md`
- Depends On:
  - `specs/features/FEAT-008-node-graph-clipping-and-vertical-arrow-anchors.md`

## Problem
The current shell layout is a fixed three-column grid and does not support a Nuke-like workflow where panes can be resized, tabbed, and arranged into horizontal/vertical splits.

## Scope
- Refactor the app shell to use an extensible docking workspace model.
- Enable pane resizing via splitters.
- Enable tab stacks and docking operations (left/right/top/bottom split insertion).
- Preserve existing editor behavior while panes are rearranged.

## Requirements
- Workspace layout must be represented by a model that supports nested splits and tab stacks.
- Users must be able to resize split regions with pointer-driven splitters.
- Users must be able to place panes as tabs in a stack and switch active tabs.
- Users must be able to dock a pane into a new split region on any side of a target stack (left, right, top, bottom).
- Docking interactions must support both drag-and-drop and command-driven actions.
- Default startup layout must feel Nuke-like, with clear separation between graph editing, viewer, and properties/tools.
- Existing graph interactions (pan/zoom, node drag, wire rendering), preview updates, and parameter editing must continue to work after layout changes.
- Layout actions must not mutate domain graph state directly.
- Any third-party docking dependency used for implementation must be free and open source.

## Acceptance Criteria
- App starts with a docked workspace that includes graph editor, viewer, and properties/tools panes.
- Dragging a splitter resizes adjacent regions and layout updates immediately.
- At least one pane can be converted into a tabbed stack with another pane and switched via tab header.
- A pane can be docked into each split direction (left/right/top/bottom) through docking actions.
- Graph/editor behavior remains functional after resizing and docking operations.
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.

## Out of Scope
- Floating/detached windows (unless explicitly pulled into scope later).
- Multi-window workspace synchronization.
- Full preset/layout manager UX beyond a single default layout.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
