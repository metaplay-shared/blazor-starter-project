using System.Text.Json.Serialization;

namespace WebClient.Testing;

/// <summary>
/// What the browser tests read from the running client, without going through the UI. Add what your tests need: a
/// property here, set in <see cref="TestStateHook.Get"/>. Only plain types (strings, numbers, bools and lists of them),
/// because WebClient.Tests compiles this file in too, without the SDK.
/// </summary>
public record TestState(
    string  ConnectionStatus,
    string? PlayerId,
    int?    TotalLogins,
    // TODO: Example content, replace with your own. The auto-clicker example's click count.
    int?    Clicks);

/// <summary>
/// The source-generated serializer for <see cref="TestState"/>, which a trimmed client can still run.
/// </summary>
[JsonSerializable(typeof(TestState))]
internal partial class TestStateJson : JsonSerializerContext { }
