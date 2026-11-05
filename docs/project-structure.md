# Project structure

The projects of the starter project, and which of its files a game built from it shares with it.

## The projects

| Path | What it is |
|---|---|
| `SharedCode/` | Game logic that both the client and the server run (namespace `Game.Logic`). `SharedCode/SharedCode.Client.csproj` compiles it for the web client against `Metaplay.Client`. |
| `Backend/SharedCode/` | Compiles the same `SharedCode/` sources for the server against `Metaplay.Cloud`. |
| `Backend/Server/` | The game server. Code only the server runs lives here (namespace `Game.Server.*`). Its runtime options are in `Config/` ([`README.md`](../Backend/Server/Config/README.md)). |
| `Backend/BotClient/` | The load-test bot client (`metaplay dev botclient`). |
| `Backend/SharedCode.Tests/`, `Backend/Server.Tests/` | Unit tests ([`testing.md`](testing.md)). |
| `WebClientBase/` | The game-independent part of the web client: the host builder, the client service base, the connection dialog, the startup splash and the base styles. |
| `WebClient/` | The game's Blazor WebAssembly app ([`web-client.md`](web-client.md)). |
| `WebClient.Tests/` | The browser tests, and client tests that need no browser. |
| `GameConfigSource/` | The game config's CSV sheets ([`README.md`](../GameConfigSource/README.md)). |
| `tools/GameConfigGen/`, `tools/GameConfigGen.Tests/` | The game config build, and its tests. |
| `tools/SerializerGen/` | Generates the serializer the web client loads. The `WebClient` build runs it. |
| `run_e2e.py`, `e2e.toml`, `tools/e2e-stack.slnx` | The browser test harness ([`testing.md`](testing.md#the-e2e-harness)). |
| `Game.slnx`, `Game.slnLaunch`, `.run/` | The solution, and the run configurations for Visual Studio and Rider. |
| `docs/design/` | The game's design documentation. |

`SharedCode/` is compiled twice because the client and server SDKs each carry their own copy of the SDK's core types,
so the base types the game's models derive from are a different .NET type on each side. A file added to `SharedCode/`
is in both builds, and neither build may reference anything the other cannot see.

The SDK lives outside the game's code. `MetaplaySDKPath` in `Directory.Build.props`, `sdkRootDir` and `$schema` in
`metaplay-project.yaml`, and the `Metaplay` folder of `Game.slnx` say where it is, and they must agree.

## Files a game shares

A game built from this project, including Metaplay's Blazor samples, keeps these files as they are:

- **The web client's base:** `WebClientBase/`, `WebClient/WebClient.csproj`, `WebClient/Components/GamePageBase.cs`,
  `WebClient/Properties/launchSettings.json`, and the empty `WebClient/wwwroot/metaplay-endpoints.json`.
- **The test harness:** `run_e2e.py`, `tools/e2e-stack.slnx`, and in `WebClient.Tests/`: `ClientHealth.cs`,
  `ClientUnderTest.cs`, `E2EStack.cs`, `Parallelism.cs`, `PlaywrightBrowserInstall.cs` and `playwright.runsettings`.
- **The tools and the build:** `tools/SerializerGen/`, `SharedCode/SharedCode.Client.csproj`,
  `Backend/Server/Server.csproj`, `Backend/Server/ServerMain.cs`, `Directory.Build.props` and `global.json`.

A game starts from these and adapts them: `e2e.toml`, `WebClient.Tests/GamePageTest.cs`,
`WebClient.Tests/CloudHostingTests.cs`, `WebClient.Tests/ConnectionOverlayTests.cs`, `tools/GameConfigGen/`,
`Backend/Server/Config/`, and the client's `Program.cs`, `Services/MetaplayClientService.cs`,
`Integration/GameOfflineServer.cs`, `wwwroot/index.html` and `wwwroot/app.css`.

The rest is the example game, marked with `TODO: Example content, replace with your own.` comments.
