using Metaplay.Core.Client;
using Metaplay.Core.Model;

namespace Game.Logic
{
    /// <summary>
    /// Registry for game-specific <see cref="ClientSlot"/> values. A ClientSlot identifies a multiplayer
    /// entity association on the client. Core slots use the low range (1-4), so use higher values here.
    /// Add a slot per multiplayer entity the client subscribes to, for example:
    /// <code>
    /// public static readonly ClientSlot Match = new ClientSlotGame(20, nameof(Match));
    /// </code>
    /// </summary>
    [MetaSerializable]
    public class ClientSlotGame : ClientSlot
    {
        public ClientSlotGame(int id, string name) : base(id, name) { }
    }
}
