using Metaplay.Core;
using Metaplay.Core.Config;
using Metaplay.Core.Model;

namespace Game.Logic
{
    // TODO: Example content, replace with your own. Everything in this file is the starter
    // project's auto-clicker example: a StringId key and a config item shaped the way a library item is shaped.

    /// <summary>
    /// Typed identifier for an <see cref="AutoClickerInfo"/> config item. Using <see cref="StringId{T}"/> instead of
    /// a raw string keeps references type-safe across configs, model state and actions.
    /// </summary>
    [MetaSerializable]
    public class AutoClickerId : StringId<AutoClickerId> { }

    /// <summary>
    /// One purchasable auto-clicker type. Its <see cref="Cost"/> (in clicks) and <see cref="ClicksPerSecond"/>
    /// production rate are designer-tunable game-config values. This is the canonical shape of a
    /// <see cref="GameConfigLibrary{TKey,TInfo}"/> item; the rows live in <c>GameConfigSource/AutoClickers.csv</c>.
    /// </summary>
    [MetaSerializable]
    public class AutoClickerInfo : IGameConfigData<AutoClickerId>
    {
        [MetaMember(1)] public AutoClickerId Id              { get; private set; }
        [MetaMember(2)] public string        DisplayName     { get; private set; }
        [MetaMember(3)] public int           Cost            { get; private set; }
        [MetaMember(4)] public int           ClicksPerSecond { get; private set; }

        public AutoClickerId ConfigKey => Id;

        // The config build needs only this: it constructs the item and assigns each member named by a column
        // header. A type whose values are derived rather than authored can instead take them through a
        // constructor tagged [MetaGameConfigBuildConstructor]; an untagged one is never used by the build.
        public AutoClickerInfo() { }
    }
}
