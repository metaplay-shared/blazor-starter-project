using Metaplay.Core;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Game.Logic.Tests
{
    /// <summary>
    /// Example test fixture that shows how to test execution of <see cref="PlayerAction"/> against
    /// the game-specific <see cref="PlayerModel"/> without needing to run the server, with the committed
    /// game config (<see cref="TestGameConfig"/>) and the helpers in <see cref="PlayerTestHelper"/>.
    /// </summary>
    // TODO: Example content, replace with your own. - every test case asserts on the auto-clicker example. The
    // helpers they use are in PlayerTestHelper and TestGameConfig.
    [TestFixture]
    public class PlayerActionTests
    {
        /// <summary> Game config loaded as fixture setup. </summary>
        static SharedGameConfig _gameConfig;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            _gameConfig = await TestGameConfig.LoadSharedAsync();
        }

        [Test]
        public void PlayerClick_IncrementsClickCounter()
        {
            PlayerModel playerModel = PlayerTestHelper.Create(_gameConfig);
            Assert.That(playerModel.NumClicks, Is.EqualTo(0));

            PlayerTestHelper.Execute(playerModel, new PlayerClick());
            PlayerTestHelper.Execute(playerModel, new PlayerClick());

            // Read from config rather than hardcoding 1: retuning the sheet must not fail a test about the
            // action, and this is what makes the config value part of what the action is asserted to do.
            Assert.That(playerModel.NumClicks, Is.EqualTo(2 * _gameConfig.Global.ClicksPerTap));
        }

        [Test]
        public void PlayerBuyAutoClicker_DeductsCostAndProducesClicksOverTime()
        {
            PlayerModel   playerModel = PlayerTestHelper.Create(_gameConfig);
            AutoClickerId cursor      = AutoClickerId.FromString("cursor");
            int           cost        = _gameConfig.AutoClickers[cursor].Cost;

            // Earn enough clicks to afford one cursor, then buy it. Clicking is driven off the configured
            // per-click award so the loop does not silently over- or under-earn if that value is retuned.
            int perTap = _gameConfig.Global.ClicksPerTap;
            int taps   = (cost + perTap - 1) / perTap;
            for (int i = 0; i < taps; i++)
                PlayerTestHelper.Execute(playerModel, new PlayerClick());
            Assert.That(playerModel.NumClicks, Is.GreaterThanOrEqualTo(cost));

            int clicksBeforePurchase = playerModel.NumClicks;
            PlayerTestHelper.Execute(playerModel, new PlayerBuyAutoClicker(cursor));
            Assert.That(playerModel.NumClicks, Is.EqualTo(clicksBeforePurchase - cost));
            Assert.That(playerModel.AutoClickers.GetValueOrDefault(cursor), Is.EqualTo(1));

            // The cursor produces 1 click/second. Each GameTick credits production for the time elapsed up to
            // the start of that tick, so drive 5 seconds of game time plus one tick to settle the final second.
            int clicksAfterPurchase = playerModel.NumClicks;
            PlayerTestHelper.Tick(playerModel, 5 * PlayerModel.TicksPerSecond + 1);
            Assert.That(playerModel.NumClicks, Is.EqualTo(clicksAfterPurchase + 5));
        }

        [Test]
        public void PlayerBuyAutoClicker_FailsWhenTooPoor()
        {
            PlayerModel playerModel = PlayerTestHelper.Create(_gameConfig);

            MetaActionResult result = PlayerTestHelper.Execute(playerModel, new PlayerBuyAutoClicker(AutoClickerId.FromString("cursor")));

            Assert.That(result, Is.EqualTo(ActionResults.NotEnoughClicks));
            Assert.That(playerModel.AutoClickers, Is.Empty);
        }
    }
}
