using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;
using System.Diagnostics;
using System.Text.Json;
using WebClient.Testing;

namespace WebClient.Tests;

/// <summary>
/// The base of the browser fixtures: where the client under test is, a check that it is this build, and a check after
/// each test that it did not stop with an error.
/// </summary>
public abstract class GamePageTest : PageTest
{
    /// <summary>
    /// The client under test's origin: the dev server's port from <c>WebClient/Properties/launchSettings.json</c>, or
    /// the harness's from <c>E2E_CLIENT_URL</c>.
    /// </summary>
    protected static readonly string ClientOrigin = E2EStack.Setting("E2E_CLIENT_URL") ?? "http://localhost:5290";

    /// <summary>
    /// A page URL on the client under test, with the query that points it at the harness's game server. The client
    /// reads that query only when it starts, so a test that reloads after in-app navigation goes to a
    /// <see cref="ClientUrl"/> again.
    /// </summary>
    protected static string ClientUrl(string route = "/", string? query = null)
    {
        string full = string.Join('&', new[] { query, E2EStack.EndpointQuery }.Where(part => !string.IsNullOrEmpty(part)));
        return full.Length == 0 ? ClientOrigin + route : $"{ClientOrigin}{route}?{full}";
    }

    /// <summary>
    /// The state of the client on <paramref name="page"/>, read from the client itself rather than from the screen
    /// (<c>WebClient/Testing/TestStateHook.cs</c>). Null while the page has no running client, such as during a reload.
    /// </summary>
    protected static async Task<TestState?> TestStateAsync(IPage page) => (await ReadTestStateAsync(page)).State;

    /// <summary>
    /// Asks the page for its client's test state. An error comes back as the reason there is no state rather than being
    /// swallowed, so a test that times out waiting for a state says why it never got one.
    /// </summary>
    private const string ReadTestStateScript =
        """
        async () => {
            if (typeof DotNet === 'undefined')
                return { state: null, error: 'the page has no .NET runtime yet' };
            try {
                return { state: await DotNet.invokeMethodAsync('WebClient', 'GetTestState'), error: null };
            } catch (e) {
                return { state: null, error: String(e) };
            }
        }
        """;

    /// <summary>The state of the client on <paramref name="page"/>, or null and the reason there is none.</summary>
    private static async Task<(TestState? State, string? Reason)> ReadTestStateAsync(IPage page)
    {
        JsonElement result;
        try
        {
            result = await page.EvaluateAsync<JsonElement>(ReadTestStateScript);
        }
        catch (PlaywrightException e)
        {
            // The page navigated away while it was asked, which a reload does.
            return (null, e.Message);
        }

        string? error = result.GetProperty("error").GetString();
        if (error != null)
            return (null, error);

        string? json = result.GetProperty("state").GetString();
        if (json == null)
            return (null, "the client service has not been created yet");

        return (JsonSerializer.Deserialize(json, TestStateJson.Default.TestState), null);
    }

    /// <summary>
    /// Wait until the state of the client on <paramref name="page"/> satisfies <paramref name="condition"/>, and fail
    /// with the last state seen if it does not get there in time. Returns the state that satisfied it.
    /// </summary>
    protected static async Task<TestState> ExpectTestStateAsync(IPage page, Func<TestState, bool> condition, string what, int timeoutMs = 30000)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (true)
        {
            (TestState? state, string? reason) = await ReadTestStateAsync(page);
            if (state != null && condition(state))
                return state;
            if (elapsed.ElapsedMilliseconds >= timeoutMs)
                Assert.Fail($"The client did not get to {what} within {timeoutMs} ms. Its last state: {state?.ToString() ?? "none, " + reason}.");
            await Task.Delay(100);
        }
    }

    /// <summary>Wait until the client on <paramref name="page"/> has a started session.</summary>
    protected static Task<TestState> ExpectConnectedAsync(IPage page, int timeoutMs = 30000)
        => ExpectTestStateAsync(page, state => state.ConnectionStatus == "Connected", "a started session", timeoutMs);

    /// <summary>The client in offline mode. A test that opens it calls <see cref="ExpectNoGameServerConnections"/>.</summary>
    protected static readonly string OfflineUrl = ClientUrl(query: "offline");

    /// <summary>
    /// Wait until the number <paramref name="counter"/> shows is at least <paramref name="atLeast"/>, and fail with
    /// the last value seen if it does not get there in time.
    /// </summary>
    protected static async Task ExpectCountToReachAsync(ILocator counter, int atLeast, int timeoutMs)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        string last = "";
        while (elapsed.ElapsedMilliseconds < timeoutMs)
        {
            last = (await counter.InnerTextAsync()).Trim();
            if (int.TryParse(last, out int value) && value >= atLeast)
                return;
            await Task.Delay(100);
        }

        Assert.Fail($"The count did not reach {atLeast} within {timeoutMs} ms; it last showed '{last}'.");
    }

    /// <summary>Fail the fixture before its first test if the client under test is not this build.</summary>
    [OneTimeSetUp]
    public static Task VerifyClientUnderTestIsThisBuild() => ClientUnderTest.VerifyIsThisBuildAsync(ClientOrigin);

    private readonly List<ClientHealth> _clients = new List<ClientHealth>();

    private int _gameServerConnections;

    [SetUp]
    public void WatchThePage() => Watch(Page);

    /// <summary>
    /// Fail the test if <c>Page</c> connects to the game server, for a test of offline mode. The URLs also carry the
    /// harness's game server ports, so a client that ignored <c>?offline</c> would connect to that server and pass
    /// every other check.
    /// </summary>
    protected void ExpectNoGameServerConnections()
    {
        Page.WebSocket += (_, webSocket) =>
        {
            if (new Uri(webSocket.Url).Port.ToString() == E2EStack.WebSocketPort)
                Interlocked.Increment(ref _gameServerConnections);
        };
    }

    /// <summary>
    /// Check <paramref name="page"/> after the test, as the fixture's own <c>Page</c> is checked. A test that opens
    /// another page passes it through here.
    /// </summary>
    protected IPage Watch(IPage page)
    {
        lock (_clients)
            _clients.Add(ClientHealth.Watch(page));
        return page;
    }

    /// <summary>
    /// Fail a passing test if Blazor's error bar showed on a page it opened, and print what the pages' clients logged
    /// when the test fails.
    /// </summary>
    [TearDown]
    public async Task CheckTheClients()
    {
        try
        {
            ClientHealth[] clients;
            lock (_clients)
                clients = _clients.ToArray();

            // A test whose assertions ran before the client threw would pass, and a later test would fail with an
            // unrelated timeout instead.
            bool errorBarShown = false;
            foreach (ClientHealth client in clients.Where(client => !client.ErrorBarExpected))
                errorBarShown |= await client.ErrorBarShownAsync();

            // What the clients logged often explains a failure of any kind.
            bool failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
            if (failed || errorBarShown)
                PrintClientOutput(clients);

            if (errorBarShown && !failed)
            {
                Assert.Fail(
                    "Blazor's error bar showed: the client threw during this test and stopped rendering. What it " +
                    "logged is in the [client] lines above.");
            }

            if (_gameServerConnections > 0 && !failed)
                Assert.Fail("The client connected to the game server in offline mode.");
        }
        finally
        {
            await CloseAfterTestAsync();
        }
    }

    /// <summary>
    /// Expect Blazor's error bar on <paramref name="page"/>, for a test of a client that is meant to stop, and leave
    /// that page out of the check after the test.
    /// </summary>
    protected async Task ExpectErrorBarAsync(IPage page)
    {
        await Expect(page.Locator("#blazor-error-ui")).ToBeVisibleAsync();
        lock (_clients)
        {
            foreach (ClientHealth client in _clients.Where(client => client.Page == page))
                client.ErrorBarExpected = true;
        }
    }

    /// <summary>
    /// Close what the test opened besides the fixture's own page, such as a browser of its own. A <c>[TearDown]</c> in
    /// the fixture would run before <see cref="CheckTheClients"/> and close the pages before they are checked.
    /// </summary>
    protected virtual Task CloseAfterTestAsync() => Task.CompletedTask;

    private static void PrintClientOutput(ClientHealth[] clients)
    {
        for (int ndx = 0; ndx < clients.Length; ndx++)
        {
            IReadOnlyList<string> entries = clients[ndx].Entries;
            if (entries.Count == 0)
                continue;

            TestContext.Out.WriteLine($"[client] page {ndx + 1} logged:");
            foreach (string entry in entries)
                TestContext.Out.WriteLine("[client]   " + entry);
        }
    }
}
