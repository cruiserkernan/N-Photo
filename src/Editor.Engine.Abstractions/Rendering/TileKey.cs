using Editor.Domain.Graph;

namespace Editor.Engine.Abstractions.Rendering;

public readonly record struct TileKey(
    NodeId NodeId,
    NodeFingerprint Fingerprint,
    int Level,
    int TileX,
    int TileY,
    string Quality);
