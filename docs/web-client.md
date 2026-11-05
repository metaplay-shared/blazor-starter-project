# The web client

The web client runs **entirely in the browser** as a Blazor WebAssembly app. The Metaplay game client (connection,
`PlayerModel`, actions) executes client-side and talks to the game server over a WebSocket; there is no server-side
Blazor circuit. `WebClient/` is the game's app, and `WebClientBase/` the game-independent part it builds on
([`project-structure.md`](project-structure.md)).

## The `net10.0-browser` target framework

`WebClient` targets `net10.0-browser`, and that target framework selects the SDK's browser flavor across the whole
`WebClient -> SharedCode.Client/WebClientBase -> Metaplay.Client` reference graph: the compiler defines `BROWSER`,
which switches the SDK to the `ClientWebSocket` transport and the IndexedDB blob store, and compiles out the runtime
serializer codegen (unsupported on mono-wasm). `Metaplay.Client` multi-targets `net10.0;net10.0-browser`;
`SharedCode.Client` does too, so `tools/SerializerGen` can reference its plain-`net10.0` flavor. No extra
command-line properties are needed.

The client projects target only the browser, so a plain .NET project cannot reference them. Tests of client logic
compile the client's files in instead ([`testing.md`](testing.md#client-tests-without-a-browser)).

## The pre-built serializer

mono-wasm cannot generate the Metaplay serializer at runtime, so the client loads a pre-built
`Metaplay.Generated.Browser.dll`, the name the browser build's engine integration reports as its generated-serializer
assembly. The `WebClient` build generates it: the `GenerateBrowserSerializer` target in `WebClient.csproj` runs
`tools/SerializerGen` into `obj/` whenever the generator's inputs (the game code, the SDK and the tool) are newer than
the DLL. The generator skips the Roslyn compile when the generated source has not changed, so a build where no
`[MetaSerializable]` type changed stays cheap. Nothing is committed.

## The built-in game config archive

The client looks for a built-in config archive at `wwwroot/Assets/SharedGameConfig.mpa`, the same file the Unity
editor writes into StreamingAssets during a player build. Online it is only a cache the SDK falls back from to the
CDN, but [offline mode](#offline-mode) has nothing else to load the config from.

The config build writes it alongside the server's `StaticGameConfig.mpa`, in the form a live server hands out:
re-serialized so `[ServerOnly]` members are stripped. **Rebuild both whenever a sheet in `GameConfigSource/` or a
config class changes:** `dotnet run --project tools/GameConfigGen`.

## Offline mode

`?offline` runs the client with no game server: the session, player model and ticking are served in-process by
`WebClient/Integration/GameOfflineServer.cs`, against the built-in config archive. It stands in for the server's
`PlayerActor`, so a server-side reaction to a player action goes in both.

The mode is the SDK's `offline` web client environment, whose empty `ServerHost` is what the SDK reads as offline. It
is never detected from the page, so `?offline` is the only way in. The offline player is kept in the browser's
`localStorage` between reloads; Reset deletes it.

## The start query

The SDK reads the page's query once, at startup: `?offline`, and `?wsPort=` and `?cdnPort=`, which point the local
environment at a server on other ports. In-app links and navigations go to plain paths, so the address loses that
query. To reload, call `StartPage.Reload` (`WebClientBase/Services/StartPage.cs`): it reloads at the app root with
the query the page was loaded with. Read the game's own query settings once at startup too, so they hold on pages
reached later.

## Frame loop

The client runs on the browser's own frame loop. The SDK's `BrowserFrameLoop` runs a frame on each of the browser's
animation frames once the client service starts it: the SDK takes in what the server sent, advances the game and sends
the player's pending actions. Then the open page's own per-frame work runs, in its `OnFrame` override
(`WebClient/Components/GamePageBase.cs`), and the client service updates the UI. A slow timer keeps frames running
when the browser leaves a visible page without animation frames, such as a covered window.

A hidden page gets no animation frames, so it counts as the game being paused, as a mobile game in the background
does. The SDK sends what is pending, tells the server, which keeps the session waiting for a while, and saves the
offline player. When the page is shown again, the session continues; after a long time hidden, the client
reconnects.

## Pages and rendering

A routed page derives from `WebClient/Components/GamePageBase.cs` (`@inherits GamePageBase`), which starts the client
service when the page opens, so a page opened by its own URL connects too, and renders the page again on the client
service's `OnStateChanged`.

While connected, the service raises `OnStateChanged` on each frame, so whatever the player model or the clock changes
is on screen within a frame. A game with little that changes on screen can render only on a change instead: override
`RenderEveryFrame` in `WebClient/Services/MetaplayClientService.cs` to return false. The page then renders when the
connection status changes and after a client action. Any other change the UI shows, in `GameTick` or in a server
action, on the player model or another entity's model, must then reach the UI through `RequestRender()`, typically
called from a model client listener such as the service's `IPlayerModelClientListener` members.

## The client service

`WebClient/Services/MetaplayClientService.cs` is the game's client service. It derives from
`WebClientBase/Services/MetaplayClientServiceBase.cs`, which runs the frames and the connection:

- `ExecuteAction` runs a player action at once and returns its result, or null with no session or when the action
  threw. The SDK sends it at the start of the next frame.
- `OnSessionStarted` is where the game wires up its model listeners. `OnSessionEnded` is where it drops what
  belonged to the session, such as requests whose responses will not arrive.
- `IsUnhealthy` is set while the session's connection is degraded and the SDK tries to restore it. The debug bar
  then says "Unhealthy", and the game stays usable, because the restore often succeeds.
- `Reset()` disconnects and deletes the credentials and the offline player. Reload afterwards to start as a new
  player.

## Connection handling

The client service connects, and reconnects after a lost session as the SDK recommends. Pages do not check the
connection: until the first session has a player to draw, a page shows `StartupSplash`, and after that
`ConnectionOverlay`, mounted once in `App.razor`, shows a lost session over the whole app as a modal dialog, whatever
layout a page uses.

What happens after a loss follows the SDK's `ConnectionLostEvent`. When its `AutoReconnectRecommended` is set, the
client reconnects on its own, with a "Reconnecting…" dialog, up to three times in a row and waiting longer each time.
Otherwise the dialog shows the SDK's reason (`EnglishLocalizedReason`) and waits for the player: it offers
"Reconnect" when `IsRetryable` says reconnecting can help, "Play here" when another tab or device took over the
session (`ConnectionLostReason.LoggedInElsewhere`), and "Reload" when the client is too old for the server. A desync
of a multiplayer entity's model is a game bug, and gets the SDK's default handling: the dialog offers Reconnect,
which fetches a fresh model.

To reconnect on the client's own in more cases, override `ShouldReconnectAutomatically` in
`WebClient/Services/MetaplayClientService.cs`, and to change how many times in a row, `MaxAutoReconnects`.

## Styles

The styles are plain CSS, with no CSS framework. Text uses the system's UI font, and the display font is served with
the client, so the page loads no styles or fonts from another origin.

- `WebClientBase/wwwroot/css/app-base.css` holds the base styles: the display font (Lilita One, in
  `WebClientBase/wwwroot/fonts/` with its license), a short element reset, the page frame, the motion toolkit
  (`.font-display`, `.animate-*`, `.btn-glossy`) and the styles of `WebClientBase`'s components, whose classes are
  prefixed `mp-`.
- `WebClient/wwwroot/app.css` holds the game's styles. It loads after `app-base.css`, so its rules win over the base
  styles at equal specificity.
- The colors are the theme's: `ThemeColors` in `WebClient/Program.cs` sets them, and `App.razor` renders them into
  the page as CSS variables (`--bg-primary`, `--text-secondary`, `--accent-blue`, ...). Use the variables rather than
  color values, so a theme change reaches all screens.

## Running it

`metaplay dev server` exposes the game server's WebSocket on port 9380, which the client's local environment points
at. `dotnet run --project WebClient/WebClient.csproj` serves the client on http://localhost:5290, and `dotnet watch`
rebuilds it when a file changes and applies the change to the open page where it can, or reloads it.

Publishing the client, deploying it, opening it from another machine and installing it as an app are in
[`publishing.md`](publishing.md).

## Notes

- The credentials persist across reloads in the SDK's browser blob store, so the same guest account is reused. Reset
  deletes them.
- The `ClientPlatform` reported to the server is `WebGL`, a coarse telemetry signal the SDK uses for both in-browser
  runtimes (Unity WebGL and managed WASM).
- Static assets under `wwwroot/` are loaded over HTTP via `HttpClient`, not from the server's file system.
