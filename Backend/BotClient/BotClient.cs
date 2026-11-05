using Game.Logic;
using Metaplay.BotClient;
using Metaplay.Cloud.Entity;
using Metaplay.Core;
using Metaplay.Core.InAppPurchase;
using Metaplay.Core.InGameMail;
using Metaplay.Core.Message;
using System;
using System.Threading.Tasks;

namespace Game.BotClient
{
    public enum BotClientState
    {
        Connecting,
        Main,
    }

    // BotClient

    [EntityConfig]
    internal class BotClientConfig : BotClientConfigBase
    {
        public override Type EntityActorType => typeof(BotClient);
    }

    public class BotClient : BotClientBase
    {
        BotClientState              State { get; set; } = BotClientState.Connecting;

        PlayerModel      _playerModel => (PlayerModel)PlayerContext.Model;
        SharedGameConfig _gameConfig;

        public BotClient()
        {
        }

        protected override void PreStart()
        {
            base.PreStart();
        }

        protected override void RegisterHandlers()
        {
            base.RegisterHandlers();
        }

        protected override string GetCurrentStateLabel() => State.ToString();

        protected override Task OnUpdate()
        {
            // Tick current state (when connected)
            switch (State)
            {
                case BotClientState.Main:
                    TickMainState();
                    break;
            }

            return Task.CompletedTask;
        }

        protected override Task OnNetworkMessage(MetaMessage message)
        {
            // The base class handles the session and player-action messages before they reach here.
            _log.Warning("Unknown message received: {Message}", PrettyPrint.Compact(message));

            return Task.CompletedTask;
        }

        protected override Task OnSessionStartedAsync(BotSessionStartedArgs args)
        {
            _gameConfig = (SharedGameConfig)args.GameConfig;
            State = BotClientState.Main;

            return Task.CompletedTask;
        }

        void TickMainState()
        {
            // TODO: Example content, replace with your own. The method is what the bot does every update;
            // its body exercises the example loop under load - click every update, and buy the cheapest
            // affordable auto-clicker so the buy action and auto-clicker production both get traffic.
            PlayerContext.ExecuteAction(new PlayerClick());

            AutoClickerInfo cheapest = null;
            foreach (AutoClickerInfo info in _gameConfig.AutoClickers.Values)
            {
                if (cheapest == null || info.Cost < cheapest.Cost)
                    cheapest = info;
            }

            if (cheapest != null && _playerModel.NumClicks >= cheapest.Cost)
                PlayerContext.ExecuteAction(new PlayerBuyAutoClicker(cheapest.Id));
        }

        [MessageHandler]
        void HandleInitializeBot(BotCoordinator.InitializeBot _)
        {
            _log.Debug("Initializing bot");
        }

        static async Task<int> Main(string[] cmdLineArgs)
        {
            using (BotClientMain program = new BotClientMain())
                return await program.RunBotsAsync(cmdLineArgs);
        }
    }
}
