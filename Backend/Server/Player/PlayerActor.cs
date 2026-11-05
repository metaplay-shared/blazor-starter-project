using Game.Logic;
using Metaplay.Cloud.Entity;
using Metaplay.Core.Config;
using Metaplay.Server;
using System;

namespace Game.Server.Player
{
    [EntityConfig]
    public class PlayerConfig : PlayerConfigBase
    {
        public override Type EntityActorType => typeof(PlayerActor);
    }

    /// <summary>
    /// Server-side entity actor representing a single player. Owns the authoritative <see cref="PlayerModel"/>
    /// and reacts to player actions via <see cref="IPlayerModelServerListener"/>. The starter project keeps
    /// this minimal; add server-side game logic here.
    /// </summary>
    public sealed class PlayerActor : PlayerActorBase<PlayerModel>, IPlayerModelServerListener
    {
        public PlayerActor()
        {
        }

        /// <summary>
        /// The SharedGameConfig that contains any A/B tests that affect this user player.
        /// This is the same instance as <c>Model.GameConfig</c>.
        /// This is only available after PostLoad has been invoked, or rather in most methods outside Initialize, InitializePersisted, PostLoad, and the constructor!
        /// </summary>
        private SharedGameConfig SharedGameConfig => (SharedGameConfig)SpecializedGameConfig.SharedConfig;

        protected override string RandomNewPlayerName()
        {
            return GuestNames.Random(new Random());
        }

        protected override void OnSwitchedToModel(PlayerModel model)
        {
            model.ServerListener = this;
        }

        // TODO: Example content, replace with your own.
        void IPlayerModelServerListener.OnAutoClickerPurchased(AutoClickerId autoClickerId)
        {
            // Example server-side reaction to a player action: here we just log, but this is where you'd
            // award analytics events, notify other entities, etc. Server listeners must not mutate model state.
            AutoClickerInfo info = SharedGameConfig.AutoClickers[autoClickerId];
            _log.Info("Player bought auto-clicker {AutoClicker} (+{Rate} clicks/s)", info.DisplayName, info.ClicksPerSecond);
        }
    }
}
