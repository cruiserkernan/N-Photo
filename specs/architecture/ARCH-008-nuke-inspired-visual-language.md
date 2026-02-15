# ARCH-008: Nuke-Inspired Visual Language

- Status: Implemented
- Last Updated: 2026-02-15
- Scope: App-shell visual system for a darker, cleaner, Nuke-inspired editor UI in Avalonia

## Goals
- Establish a consistent visual language across toolbar, dock headers, panels, node canvas, and inspector controls.
- Improve readability, hierarchy, and perceived quality without changing editor domain behavior.
- Keep styling centralized and reusable so future panels inherit the same design tokens.

## Decisions
- Define a shared app-level style token set (colors, spacing, radii, typography sizes, border strengths) in `App.axaml`.
- Use a dark, neutral base palette with restrained accent usage for active/selected states.
- Keep panel identity clear through subtle chrome treatment (header contrast, panel body separation, consistent borders).
- Preserve existing docking behavior and panel composition from `ARCH-007`; this change is visual and ergonomic.
- Prefer built-in Avalonia theming/styling primitives over adding new UI dependencies.
- Bundle a free, permissively licensed font family (recommended: IBM Plex Sans, SIL OFL) for app UI typography.
- Split major workspace view compositions into dedicated `UserControl` files (graph, viewer, properties, and toolbar host sections) and keep `MainWindow` focused on shell composition + wiring.
- Remove debug-oriented shell controls from the primary UI surface and keep only production-facing actions.
- Add subtle motion feedback for hover/selection/state changes using lightweight Avalonia transitions.

## Boundaries
- `App` owns visual styling and layout polish.
- `Editor.Domain`, `Editor.Engine`, `Editor.Imaging`, and `Editor.IO` remain unaffected.
- Workspace docking mechanics and graph behavior are out of scope unless needed for presentation clarity only.

## Refactoring Guidance
- Consolidate repeated hard-coded brushes/margins into shared style resources.
- Keep style keys and selectors explicit to avoid accidental broad theme overrides.
- Move panel-local XAML into view-specific files to reduce merge conflicts and improve maintainability.

## Open Questions
- None.

## Validation
- `dotnet build NPhoto.slnx -c Debug` succeeds.
- `dotnet test NPhoto.slnx -c Debug --no-build` succeeds.
