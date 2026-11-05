using Game.Logic;
using Metaplay.Core.Player;
using WebClientBase.Integration;

namespace WebClient.Integration;

/// <summary>
/// The game's offline server. This stands in for <c>PlayerActor</c> in offline mode, so a server-side reaction
/// to a player action goes here as well as in <c>PlayerActor</c>.
/// </summary>
public class GameOfflineServer : BlazorOfflineServer, IPlayerModelServerListener
{
    SharedGameConfig SharedGameConfig => (SharedGameConfig)_gameConfig;

    protected override void SetSelfAsPlayerListener(IPlayerModelBase playerBase)
    {
        ((PlayerModel)playerBase).ServerListener = this;
    }

    // TODO: Example content, replace with your own.
    void IPlayerModelServerListener.OnAutoClickerPurchased(AutoClickerId autoClickerId)
    {
        // Offline counterpart of PlayerActor's reaction. To answer with a server action, enqueue it as
        // PostponeAction(() => EnqueueServerAction(action)) so that, like PlayerActor, it answers only after the
        // client has flushed the action.
        AutoClickerInfo info = SharedGameConfig.AutoClickers[autoClickerId];
        _log.Info("Player bought auto-clicker {AutoClicker} (+{Rate} clicks/s)", info.DisplayName, info.ClicksPerSecond);
    }
}
