using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WebClient.Testing;

namespace WebClient.Tests;

// TODO: Example content, replace with your own. The tests in this file are the auto-clicker example seen through the
// browser, against the game server and in offline mode. What is worth keeping is that a player action reaches the
// model and the screen, and in offline mode that the game runs against the built-in archive.

/// <summary>
/// E2E tests for the example game screen, against the game server and in offline mode.
/// Requires a running game server and web client: a hand-started pair, or the one run_e2e.py starts.
/// </summary>
[TestFixture]
public class ExampleGameTests : GamePageTest
{
    private static readonly string BaseUrl = ClientUrl();

    /// <summary>
    /// Open the game against the game server, or in offline mode against the built-in game config archive.
    /// </summary>
    private async Task GotoGameAsync(bool offline)
    {
        if (offline)
            ExpectNoGameServerConnections();
        await Page.GotoAsync(offline ? OfflineUrl : BaseUrl);
    }

    [Test]
    public async Task GameScreen_ShowsWhenConnected()
    {
        await GotoGameAsync(offline: false);

        // Once connected, the example game screen shows the "Click" button.
        var clickButton = Page.GetByRole(AriaRole.Button, new() { Name = "Click" });
        await Expect(clickButton).ToBeVisibleAsync(new() { Timeout = 20000 });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ClickButton_IncrementsCounter(bool offline)
    {
        await GotoGameAsync(offline);

        var clickButton = Page.GetByRole(AriaRole.Button, new() { Name = "Click" });
        await Expect(clickButton).ToBeVisibleAsync(new() { Timeout = 30000 });

        // A fresh guest session starts at zero; clicking runs the PlayerClick action, which increments
        // the on-screen counter.
        var counter = Page.GetByTestId("clicks-counter");
        await Expect(counter).ToHaveTextAsync("0");

        // Not an exact "1": the per-click award is a published config value (Global.csv's ClicksPerTap), so
        // asserting the count is non-zero keeps this test about the click reaching the model rather than
        // about the current tuning. The exact arithmetic is covered by the unit suite, which reads the value.
        await clickButton.ClickAsync();

        // Check the UI: the counter on screen, found by its data-testid.
        await Expect(counter).Not.ToHaveTextAsync("0");

        // Alternatively, for state the UI does not show, check the client's state directly: a TestState property
        // (WebClient/Testing/TestState.cs), here the model's click count.
        await ExpectTestStateAsync(Page, (TestState state) => state.Clicks > 0, "a click in the model");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AutoClicker_CanBeBoughtAndThenProducesClicks(bool offline)
    {
        await GotoGameAsync(offline);

        var clickButton = Page.GetByRole(AriaRole.Button, new() { Name = "Click" });
        await Expect(clickButton).ToBeVisibleAsync(new() { Timeout = 30000 });

        // The auto-clicker's cost and production rate come from the game config the server, or in offline mode the
        // offline server, loaded. The cheapest auto-clicker (Cursor, 25 clicks) is disabled until the player can
        // afford it, and its hover text says why.
        var cursor = Page.GetByRole(AriaRole.Button, new() { Name = "Cursor" });
        await Expect(cursor).ToBeDisabledAsync();
        await Expect(cursor).ToHaveAttributeAsync("title", "You need 25 clicks to buy this.");

        // Earn enough clicks to afford it.
        for (int i = 0; i < 25; i++)
            await clickButton.ClickAsync();
        await Expect(cursor).ToBeEnabledAsync();
        await Expect(cursor).Not.ToHaveAttributeAsync("title", new Regex("."));

        // Buying it runs the PlayerBuyAutoClicker action: the per-second production rate now reflects the
        // cursor's config rate (1/s), shown in the shop header.
        await cursor.ClickAsync();
        await Expect(Page.GetByText("+1 / s", new() { Exact = true })).ToBeVisibleAsync();

        // The purchase spent all 25 clicks (counter back to 0); the cursor then produces clicks on its own,
        // so the counter keeps climbing without any further input. Production happens in GameTick, not in an action,
        // so this also checks that the page keeps rendering with no input, frame after frame.
        var counter = Page.GetByTestId("clicks-counter");
        await ExpectCountToReachAsync(counter, atLeast: 3, timeoutMs: 15000);
    }
}
