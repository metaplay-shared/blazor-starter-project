using Metaplay.Core.Config;
using Metaplay.Core.Model;

namespace Game.Logic
{
    /// <summary>
    /// Global game configuration values (singleton settings, not item libraries). Built from
    /// <c>GameConfigSource/Global.csv</c>, which is written as <c>Member,Value</c> rows rather than as a table.
    /// <para>
    /// Add game-wide tunables here as the game grows. A value belongs here when a designer could reasonably
    /// want it different and there is exactly one of it; anything with an id per row belongs in a library.
    /// </para>
    /// </summary>
    [MetaSerializable]
    public class GlobalConfig : GameConfigKeyValue<GlobalConfig>
    {
        /// <summary>
        /// Clicks granted by one <see cref="PlayerClick"/>. Publishing a new value changes what a click is worth
        /// for players already in a session, with no client build and no server restart — which is the point of
        /// keeping tunables in the config archive rather than in code.
        /// </summary>
        // TODO: Example content, replace with your own. The class is a real extension point; this member is not.
        [MetaMember(1)] public int ClicksPerTap = 1;
    }
}
