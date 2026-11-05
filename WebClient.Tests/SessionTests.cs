using System.Collections.Concurrent;
using WebClient.Testing;

namespace WebClient.Tests;

/// <summary>
/// E2E tests for the player's session, whatever game the client shows: it starts against the game server or in offline
/// mode (<c>?offline</c>, the in-process offline server with the built-in game config archive), and a reload brings
/// the same player back.
/// Requires a running game server and web client: a hand-started pair, or the one run_e2e.py starts. The offline cases
/// need only the web client.
/// <para>
/// The player's credentials and offline state persist in the browser, so these rely on each test getting a fresh
/// browser context (which Playwright's <c>PageTest</c>, under <see cref="GamePageTest"/>, provides) to start from a
/// new player.
/// </para>
/// </summary>
[TestFixture]
public class SessionTests : GamePageTest
{
    [TestCase("")]
    [TestCase("offline")]
    public async Task StartsASession(string query)
    {
        if (query.Length > 0)
            ExpectNoGameServerConnections();
        await Page.GotoAsync(ClientUrl(query: query));

        await ExpectConnectedAsync(Page);
    }

    /// <summary>
    /// A reload logs the same player in again: from the stored credentials against the game server, and from the
    /// saved offline state in offline mode.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Reload_KeepsThePlayer(bool offline)
    {
        if (offline)
            ExpectNoGameServerConnections();
        await Page.GotoAsync(offline ? OfflineUrl : ClientUrl());
        TestState first = await ExpectConnectedAsync(Page);
        Assert.That(first.TotalLogins, Is.EqualTo(1), "a new player's first session");

        await Page.ReloadAsync();

        // Every session start counts a login, on the game server and in offline mode.
        TestState reloaded = await ExpectTestStateAsync(Page, state => state.ConnectionStatus == "Connected" && state.TotalLogins == 2, "a second login");
        Assert.That(reloaded.PlayerId, Is.EqualTo(first.PlayerId));
    }

    // index.html preloads the endpoints file, and the client fetches it on a local page too, where it is the empty one
    // in wwwroot/. Without the file the browser logs a 404, and with a preload that nothing uses, a warning.
    [Test]
    public async Task UsesThePreloadedEndpointsFile()
    {
        ConcurrentQueue<string> problems = new ConcurrentQueue<string>();
        Page.Response += (_, response) =>
        {
            if (response.Url.EndsWith("/metaplay-endpoints.json") && !response.Ok)
                problems.Enqueue($"{response.Url}: HTTP {response.Status}");
        };
        Page.RequestFailed += (_, request) =>
        {
            if (request.Url.EndsWith("/metaplay-endpoints.json"))
                problems.Enqueue($"{request.Url}: {request.Failure}");
        };
        Page.Console += (_, message) =>
        {
            if (message.Text.Contains("metaplay-endpoints.json"))
                problems.Enqueue($"console.{message.Type}: {message.Text}");
        };

        await Page.GotoAsync(ClientUrl());

        // Chrome warns about an unused preload 3 seconds after the load event.
        await Page.WaitForTimeoutAsync(5000);

        Assert.That(problems, Is.Empty);
    }
}
