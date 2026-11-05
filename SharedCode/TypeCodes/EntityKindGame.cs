using Metaplay.Core;

namespace Game.Logic
{
    /// <summary>
    /// Registry for game-specific <see cref="EntityKind"/> values. The game-specific range is [100, 300).
    /// Add server-side multiplayer entities (e.g. a matchmaking lobby, a match, a guild) here, for example:
    /// <code>
    /// public static readonly EntityKind Match = EntityKind.FromValue(100);
    /// </code>
    /// </summary>
    [EntityKindRegistry(100, 300)]
    public static class EntityKindGame
    {
    }
}
