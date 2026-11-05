using Metaplay.Core.Model;
using Metaplay.Core.Player;

namespace Game.Logic
{
    /// <summary>
    /// Game-specific player action base class, which attaches all game-specific actions to <see cref="PlayerModel"/>.
    /// </summary>
    public abstract class PlayerAction : PlayerActionCore<PlayerModel>
    {
    }

    /// <summary>
    /// Game-specific <see cref="PlayerUnsynchronizedServerActionCore{TModel}"/>
    /// </summary>
    public abstract class PlayerUnsynchronizedServerAction : PlayerUnsynchronizedServerActionCore<PlayerModel>
    {
    }

    /// <summary>
    /// Game-specific <see cref="PlayerSynchronizedServerActionCore{TModel}"/>
    /// </summary>
    public abstract class PlayerSynchronizedServerAction : PlayerSynchronizedServerActionCore<PlayerModel>
    {
    }

    /// <summary>
    /// Registry for game-specific ActionCodes, used by the individual PlayerAction classes. Action codes must
    /// be unique within the game.
    /// </summary>
    public static class ActionCodes
    {
        // TODO: Example content, replace with your own. The registry is a real extension point; these
        // two codes are not. Action codes must stay unique within the game.
        public const int PlayerClick           = 5001;
        public const int PlayerBuyAutoClicker  = 5002;
    }

    /// <summary>
    /// Game-specific <see cref="MetaActionResult"/> values. Descriptive names help diagnose failures in logs
    /// and the LiveOps Dashboard.
    /// </summary>
    public static class ActionResults
    {
        // TODO: Example content, replace with your own. The registry is a real extension point; these
        // two results are not.
        public static readonly MetaActionResult NoSuchAutoClicker = new MetaActionResult(nameof(NoSuchAutoClicker));
        public static readonly MetaActionResult NotEnoughClicks   = new MetaActionResult(nameof(NotEnoughClicks));
    }

    // TODO: Example content, replace with your own.
    /// <summary>
    /// Example action: increments the player's click counter by <see cref="GlobalConfig.ClicksPerTap"/>. This
    /// is the minimal end-to-end example of a player action — the client executes it optimistically and the
    /// server re-runs it authoritatively, both arriving at the same <see cref="PlayerModel.NumClicks"/>.
    /// Replace with your own game actions.
    /// </summary>
    [ModelAction(ActionCodes.PlayerClick)]
    public class PlayerClick : PlayerAction
    {
        public PlayerClick() { }

        public override MetaActionResult Execute(PlayerModel player, bool commit)
        {
            if (commit)
            {
                player.NumClicks += player.GameConfig.Global.ClicksPerTap;
                player.ClientListener.OnClicksChanged(player.NumClicks);
            }

            return MetaActionResult.Success;
        }
    }

    // TODO: Example content, replace with your own.
    /// <summary>
    /// Example transactional action: spend clicks to buy one auto-clicker of the given type. The cost is read
    /// from game config inside Execute (never trusted from the client), and the spend and the grant happen in
    /// the same action so a hacked client cannot get one without the other.
    /// </summary>
    [ModelAction(ActionCodes.PlayerBuyAutoClicker)]
    public class PlayerBuyAutoClicker : PlayerAction
    {
        public AutoClickerId AutoClickerId { get; private set; }

        public PlayerBuyAutoClicker() { }
        public PlayerBuyAutoClicker(AutoClickerId autoClickerId) { AutoClickerId = autoClickerId; }

        public override MetaActionResult Execute(PlayerModel player, bool commit)
        {
            AutoClickerInfo info = player.GameConfig.AutoClickers.GetValueOrDefault(AutoClickerId);
            if (info == null)
                return ActionResults.NoSuchAutoClicker;
            if (player.NumClicks < info.Cost)
                return ActionResults.NotEnoughClicks;

            if (commit)
            {
                // Settle production at the current rate before the purchase changes it.
                player.SettleAutoClickerProduction(player.CurrentTime);

                player.NumClicks -= info.Cost;
                player.AutoClickers[AutoClickerId] = player.AutoClickers.GetValueOrDefault(AutoClickerId) + 1;

                player.ClientListener.OnClicksChanged(player.NumClicks);
                player.ServerListener.OnAutoClickerPurchased(AutoClickerId);
            }

            return MetaActionResult.Success;
        }
    }
}
