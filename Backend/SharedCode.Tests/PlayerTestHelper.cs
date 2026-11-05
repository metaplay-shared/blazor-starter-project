using Metaplay.Core;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using System;

namespace Game.Logic.Tests
{
    /// <summary>
    /// Helpers to test actions and ticks against a <see cref="PlayerModel"/> without a server, with the game config
    /// the test passes in, typically <see cref="TestGameConfig"/>.
    /// </summary>
    public static class PlayerTestHelper
    {
        /// <summary>
        /// Create a <see cref="PlayerModel"/> in its initial state. In more complex tests, you probably want to support
        /// initializing the model to various states.
        /// </summary>
        public static PlayerModel Create(SharedGameConfig gameConfig)
        {
            PlayerModel playerModel = PlayerModelUtil.CreateNewPlayerModel<PlayerModel>(
                MetaTime.FromDateTime(new DateTime(2021, 5, 30, 19, 57, 17, DateTimeKind.Utc)),
                gameConfig,
                playerId: EntityId.CreateRandom(EntityKindCore.Player),
                name: "Example name");

            // Initialize logic version to latest supported
            playerModel.LogicVersion = IntegrationRegistry.Get<IMetaplayCoreOptionsProvider>().Options.SupportedLogicVersions.MaxVersion;

            // Simulate initial login to initialize resources
            playerModel.OnInitialLogin();

            return playerModel;
        }

        /// <summary>
        /// Execute <paramref name="action"/> against <paramref name="playerModel"/>: a client action or a server action.
        /// </summary>
        public static MetaActionResult Execute(PlayerModel playerModel, PlayerActionBase action)
        {
            return action.InvokeExecute(playerModel, commit: true);
        }

        /// <summary>
        /// Progress <paramref name="playerModel"/> by <paramref name="numTicks"/> ticks.
        /// </summary>
        public static void Tick(PlayerModel playerModel, int numTicks)
        {
            for (int ndx = 0; ndx < numTicks; ndx++)
                playerModel.Tick(NullChecksumEvaluator.Context);
        }
    }
}
