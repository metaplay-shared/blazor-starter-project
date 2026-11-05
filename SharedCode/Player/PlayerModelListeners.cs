namespace Game.Logic
{
    /// <summary>
    /// Hooks the server-side PlayerActor can implement to react to player actions (e.g. to talk to other
    /// entities or trigger side effects). Add methods as your game needs them.
    /// </summary>
    public interface IPlayerModelServerListener
    {
        // TODO: Example content, replace with your own. The interface is a real extension point; this member is not.
        /// <summary> Triggered by <see cref="PlayerBuyAutoClicker"/> after an auto-clicker is purchased. </summary>
        void OnAutoClickerPurchased(AutoClickerId autoClickerId);
    }

    /// <summary>
    /// Hooks the client can implement to react to model changes (typically to re-render the UI).
    /// </summary>
    public interface IPlayerModelClientListener
    {
        // TODO: Example content, replace with your own. The interface is a real extension point; these members are not.
        /// <summary> Triggered when the click counter changes, such as <see cref="PlayerClick"/>, and by auto-clicker production. </summary>
        void OnClicksChanged(int numClicks);
    }

    public class EmptyPlayerModelServerListener : IPlayerModelServerListener
    {
        public static readonly EmptyPlayerModelServerListener Instance = new EmptyPlayerModelServerListener();

        // TODO: Example content, replace with your own.
        public void OnAutoClickerPurchased(AutoClickerId autoClickerId) { }
    }

    public class EmptyPlayerModelClientListener : IPlayerModelClientListener
    {
        public static readonly EmptyPlayerModelClientListener Instance = new EmptyPlayerModelClientListener();

        // TODO: Example content, replace with your own.
        public void OnClicksChanged(int numClicks) { }
    }
}
