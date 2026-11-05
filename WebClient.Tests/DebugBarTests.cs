using Microsoft.Playwright;
using WebClient.Testing;

namespace WebClient.Tests;

/// <summary>
/// E2E tests for the debug bar's controls (WebClientBase's MainLayout): Reset, against the game server and in offline
/// mode. These go with the debug bar: a game that removes the bar removes these tests too.
/// Requires a running game server and web client: a hand-started pair, or the one run_e2e.py starts.
/// </summary>
[TestFixture]
public class DebugBarTests : GamePageTest
{
    private static readonly string BaseUrl = ClientUrl();

    /// <summary>
    /// Reset starts a new player and reloads the client at its start page, with the query the page was loaded with, so
    /// a client the harness pointed at its own game server stays on it.
    /// </summary>
    [Test]
    public async Task Reset_StartsANewPlayer_OnTheSameServer()
    {
        await Page.GotoAsync(BaseUrl);
        TestState first = await ExpectConnectedAsync(Page);

        // What a link to another page of a larger game leaves in the address bar: another route, without the query.
        await Page.EvaluateAsync("history.replaceState(null, '', 'elsewhere')");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();

        await ExpectTestStateAsync(Page, state => state.ConnectionStatus == "Connected" && state.PlayerId != first.PlayerId, "a session as a new player");
        Assert.That(Page.Url, Is.EqualTo(BaseUrl));
    }

    [Test]
    public async Task OfflineMode_Reset_StartsAFreshPlayer_StillOffline()
    {
        ExpectNoGameServerConnections();
        await Page.GotoAsync(OfflineUrl);
        await ExpectConnectedAsync(Page);

        // The offline server gives every player the same id, so the login count is what tells two players apart: a
        // reload logs the player in a second time, and a new player starts again from one.
        await Page.ReloadAsync();
        await ExpectTestStateAsync(Page, state => state.ConnectionStatus == "Connected" && state.TotalLogins == 2, "a second login");

        // The offline player lives in the saved offline state, not under the credentials, so Reset deletes both.
        // Otherwise the reload would bring back the old player.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();

        // The reload goes to the start page with the query the page was loaded with, so it is offline mode again,
        // with a new player.
        await ExpectTestStateAsync(Page, state => state.ConnectionStatus == "Connected" && state.TotalLogins == 1, "a session as a new player");
        Assert.That(Page.Url, Is.EqualTo(OfflineUrl));
    }
}
