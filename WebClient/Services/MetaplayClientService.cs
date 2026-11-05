using System.Collections.Generic;
using System.Linq;
using Game.Logic;
using Metaplay.Client;
using Metaplay.Core.Session;
using WebClient.Testing;
using WebClientBase.Services;

namespace WebClient.Services;

/// <summary>
/// Game-specific Metaplay client service that manages the Metaplay client connection. Inherits connection
/// lifecycle management from <see cref="MetaplayClientServiceBase{TModel}"/> and implements the player model
/// client listener.
/// </summary>
public class MetaplayClientService : MetaplayClientServiceBase<PlayerModel>, IPlayerModelClientListener
{
    public MetaplayClientService()
    {
        // Lets the browser tests read this client's state (Testing/TestStateHook.cs).
        TestStateHook.Client = this;
    }

    /// <summary>
    /// Create the Metaplay client. Add multiplayer sub-clients and game delegates here as the game grows.
    /// </summary>
    protected override IMetaplayClient CreateClient()
    {
        return MetaplayClient.Create();
    }

    // TODO: Example content, replace with your own. These three members are the client's half of the
    // auto-clicker example.
    /// <summary>
    /// Example action: increment the player's click counter.
    /// </summary>
    public void Click() => ExecuteAction(new PlayerClick());

    /// <summary>
    /// The auto-clicker types available to buy, from game config. Empty until connected.
    /// </summary>
    public IEnumerable<AutoClickerInfo> AutoClickers =>
        PlayerModel?.GameConfig.AutoClickers.Values ?? Enumerable.Empty<AutoClickerInfo>();

    /// <summary>
    /// Example transactional action: spend clicks to buy an auto-clicker.
    /// </summary>
    public void BuyAutoClicker(AutoClickerId autoClickerId) => ExecuteAction(new PlayerBuyAutoClicker(autoClickerId));

    /// <summary>
    /// Called when a session has started. Wire up the client listener.
    /// </summary>
    protected override void OnSessionStarted(MetaplaySession session)
    {
        PlayerModel? playerModel = session.PlayerContext.Model as PlayerModel;
        if (playerModel != null)
            playerModel.ClientListener = this;
    }

    #region Model client listeners

    // The UI renders every frame, so nothing needs to happen here to show a change. With RenderEveryFrame overridden to
    // false, call RequestRender() here: a change the model makes outside a client action, in GameTick or in a server
    // action, then reaches the UI only through a listener.
    // TODO: Example content, replace with your own.
    void IPlayerModelClientListener.OnClicksChanged(int numClicks) { }

    #endregion
}
