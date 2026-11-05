using Metaplay.Core;
using Metaplay.Core.Config;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using System.Runtime.Serialization;

namespace Game.Logic
{
    /// <summary>
    /// State and per-player logic for a single player. This is the heart of a Metaplay game: it is
    /// kept in sync between client and server, persisted in the database, and evolved over time via
    /// <see cref="PlayerAction"/>s and per-tick logic in <see cref="GameTick"/>.
    /// <para>
    /// The starter project's example feature is a tiny idle game: <see cref="PlayerClick"/> increments
    /// <see cref="NumClicks"/>, and clicks can be spent (via <see cref="PlayerBuyAutoClicker"/>) on
    /// auto-clickers that then produce clicks over time. Replace it with your own game state.
    /// </para>
    /// </summary>
    [MetaSerializableDerived(1)]
    [SupportedSchemaVersions(1, 1)]
    public class PlayerModel : PlayerModelBase<PlayerModel, PlayerStatisticsCore>
    {
        public const int TicksPerSecond = 10;
        protected override int GetTicksPerSecond() => TicksPerSecond;

        // External services, not serialized or PrettyPrinted.
        [IgnoreDataMember] public new SharedGameConfig       GameConfig => GetGameConfig<SharedGameConfig>();
        [IgnoreDataMember] public IPlayerModelServerListener ServerListener { get; set; } = EmptyPlayerModelServerListener.Instance;
        [IgnoreDataMember] public IPlayerModelClientListener ClientListener { get; set; } = EmptyPlayerModelClientListener.Instance;

        // Player profile.
        [MetaMember(100)] public sealed override EntityId           PlayerId    { get; set; }
        [MetaMember(101), NoChecksum] public sealed override string PlayerName  { get; set; }
        [MetaMember(102)] public sealed override int                PlayerLevel { get; set; }

        // Never retype or reuse a member id: stored players break. Retire old ids in [MetaBlockedMembers].

        // TODO: Example content, replace with your own. The three state members here, and the two
        // production methods at the end of the class, exist only for the auto-clicker example.
        // The overrides in between (GameInitializeNewPlayerModel, GameTick, GameFastForwardTime) are required
        // SDK extension points: keep the methods, replace what their bodies do.
        [MetaMember(110)] public int NumClicks { get; set; }

        // Auto-clickers owned, keyed by config id; each produces clicks over time (see SettleAutoClickerProduction).
        [MetaMember(111)] public MetaDictionary<AutoClickerId, int> AutoClickers { get; private set; } = new MetaDictionary<AutoClickerId, int>();

        // Timestamp up to which auto-clicker production has been credited to NumClicks.
        [MetaMember(112)] public MetaTime AutoClickersSettledAt { get; private set; }

        protected override void GameInitializeNewPlayerModel(MetaTime now, ISharedGameConfig gameConfig, EntityId playerId, string name)
        {
            PlayerId              = playerId;
            PlayerName            = name;
            AutoClickersSettledAt = now;
        }

        protected override void GameTick(IChecksumContext checksumCtx)
        {
            SettleAutoClickerProduction(CurrentTime);
        }

        protected override void GameFastForwardTime(MetaDuration elapsedTime)
        {
            // Offline production is credited analytically (not tick-by-tick), so reconnecting after a long
            // absence stays cheap. CurrentTime already reflects the post-offline "now" here.
            SettleAutoClickerProduction(CurrentTime);
        }

        /// <summary>
        /// Total clicks per second produced by all owned auto-clickers, from <see cref="SharedGameConfig"/>.
        /// </summary>
        public long GetAutoClickerRatePerSecond()
        {
            long total = 0;
            foreach ((AutoClickerId id, int count) in AutoClickers)
                total += (long)count * GameConfig.AutoClickers[id].ClicksPerSecond;
            return total;
        }

        /// <summary>
        /// Credit clicks produced by owned auto-clickers since <see cref="AutoClickersSettledAt"/>. Computed
        /// analytically from elapsed time, so it costs the same whether called every tick or once after a long
        /// offline period. Call this before changing the production rate (e.g. on purchase) so the new rate does
        /// not apply retroactively.
        /// </summary>
        public void SettleAutoClickerProduction(MetaTime now)
        {
            long clicksPerSecond = GetAutoClickerRatePerSecond();
            if (clicksPerSecond <= 0)
            {
                // Nothing producing yet: keep the baseline current so buying the first auto-clicker doesn't
                // retroactively produce for the whole time the account has existed.
                AutoClickersSettledAt = now;
                return;
            }

            long elapsedMs = (now - AutoClickersSettledAt).Milliseconds;
            if (elapsedMs <= 0)
                return;

            long produced = clicksPerSecond * elapsedMs / 1000;
            if (produced <= 0)
                return; // Less than one whole click so far; let the remainder accumulate until the next settle.

            NumClicks += (int)produced;
            ClientListener.OnClicksChanged(NumClicks);

            // Advance the baseline only by the time represented by the whole clicks credited, so the sub-click
            // remainder carries over instead of being lost.
            AutoClickersSettledAt += MetaDuration.FromMilliseconds(produced * 1000 / clicksPerSecond);
        }
    }
}
