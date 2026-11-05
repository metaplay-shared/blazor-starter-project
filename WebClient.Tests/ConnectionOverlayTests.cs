using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WebClient.Testing;

namespace WebClient.Tests;

/// <summary>
/// E2E tests for the connection overlay (WebClientBase's ConnectionOverlay, mounted once in App.razor) and the
/// connection loop behind it. Requires a running game server and web client: a hand-started pair, or the one
/// run_e2e.py starts.
/// </summary>
[TestFixture]
public class ConnectionOverlayTests : GamePageTest
{
    private static readonly string BaseUrl = ClientUrl();

    private static ILocator Dialog(IPage page, string name) => page.GetByRole(AriaRole.Alertdialog, new() { Name = name });

    /// <summary>A local port that nothing listens on.</summary>
    private static int UnusedPort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// A second tab in the same browser shares the first one's credentials, so its session takes the first one's.
    /// The first tab must not reconnect on its own, or the two would take the session from each other for as long as
    /// both are open; it offers the player the choice instead.
    /// </summary>
    [Test]
    public async Task SecondTab_SupersedesTheFirst_WhichWaitsForThePlayerToPlayHere()
    {
        int firstTabConnections = 0;
        Page.WebSocket += (_, webSocket) =>
        {
            if (new Uri(webSocket.Url).Port.ToString() == E2EStack.WebSocketPort)
                Interlocked.Increment(ref firstTabConnections);
        };
        await Page.GotoAsync(BaseUrl);
        TestState first = await ExpectConnectedAsync(Page);

        // Same browser context, so the same stored credentials: the same player, logged in once more by the game server.
        IPage second = Watch(await Context.NewPageAsync());
        await second.GotoAsync(BaseUrl);
        TestState secondTab = await ExpectConnectedAsync(second);
        Assert.That(secondTab.PlayerId, Is.EqualTo(first.PlayerId));
        Assert.That(secondTab.TotalLogins, Is.EqualTo(first.TotalLogins + 1));

        // The first tab says the game is playing elsewhere, with its one choice focused.
        ILocator firstDialog = Dialog(Page, "Playing elsewhere");
        await Expect(firstDialog).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(firstDialog).ToHaveAccessibleDescriptionAsync(new Regex("playing somewhere else"));
        ILocator playHere = firstDialog.GetByRole(AriaRole.Button, new() { Name = "Play here" });
        await Page.BringToFrontAsync();
        await Expect(playHere).ToBeFocusedAsync();

        // Had the first tab reconnected on its own, it would have opened another connection to the game server, and
        // the second would have lost its session in turn.
        await Page.WaitForTimeoutAsync(5000);
        Assert.That(firstTabConnections, Is.EqualTo(1), "The first tab reconnected on its own");
        await Expect(firstDialog).ToBeVisibleAsync();
        await Expect(second.GetByRole(AriaRole.Alertdialog)).ToHaveCountAsync(0);
        Assert.That((await TestStateAsync(second))?.ConnectionStatus, Is.EqualTo("Connected"));

        // Playing here takes the session back, and now the second tab says the game is playing elsewhere.
        await playHere.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Alertdialog)).ToHaveCountAsync(0, new() { Timeout = 20000 });
        await ExpectTestStateAsync(Page, state => state.ConnectionStatus == "Connected" && state.TotalLogins == first.TotalLogins + 2, "a third login");
        await Expect(Dialog(second, "Playing elsewhere")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// A first connection that fails shows the startup splash while it tries, then a dialog the player can retry from.
    /// Before the first session there is no game to show a "Reconnecting…" dialog over, so the retries stay behind the
    /// splash.
    /// </summary>
    [Test]
    public async Task FirstConnectionRefused_ShowsTheSplashThenADialog()
    {
        // No game server on that port, so the connection is refused. The asset CDN is the stack's own.
        await Page.GotoAsync($"{ClientOrigin}/?wsPort={UnusedPort()}&cdnPort={E2EStack.CdnPort}");
        ILocator splash = Page.GetByTestId("startup-splash");
        await Expect(splash).ToBeVisibleAsync(new() { Timeout = 20000 });

        ILocator dialog = Dialog(Page, "Disconnected");
        ILocator reconnecting = Dialog(Page, "Reconnecting…");
        System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!await dialog.IsVisibleAsync())
        {
            Assert.That(await reconnecting.CountAsync(), Is.EqualTo(0), "a reconnecting dialog showed before the first session");
            Assert.That(await splash.IsVisibleAsync(), Is.True, "the startup splash went away before the first session");
            Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(60000), "the client did not stop retrying and ask the player");
            await Task.Delay(100);
        }

        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Reconnect" })).ToBeVisibleAsync();
    }
}
