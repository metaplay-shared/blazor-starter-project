using System.Text.Json;
using Microsoft.JSInterop;
using WebClient.Services;

namespace WebClient.Testing;

/// <summary>
/// Hands the browser tests the client's <see cref="TestState"/>. The tests call <see cref="Get"/> from the page, as
/// <c>DotNet.invokeMethodAsync('WebClient', 'GetTestState')</c> (WebClient.Tests' <c>GamePageTest.TestStateAsync</c>).
/// </summary>
public static class TestStateHook
{
    /// <summary>The client service, set when it is created.</summary>
    internal static MetaplayClientService Client;

    /// <summary>The client's state as <see cref="TestState"/> JSON, or null before the client service exists.</summary>
    [JSInvokable("GetTestState")]
    public static string Get()
    {
        MetaplayClientService client = Client;
        if (client == null)
            return null;

        TestState state = new TestState(
            ConnectionStatus: client.ConnectionStatus.ToString(),
            PlayerId:         client.PlayerId?.ToString(),
            TotalLogins:      client.PlayerModel?.Stats.TotalLogins,
            // TODO: Example content, replace with your own. The auto-clicker example's click count.
            Clicks:           client.PlayerModel?.NumClicks);
        return JsonSerializer.Serialize(state, TestStateJson.Default.TestState);
    }
}
