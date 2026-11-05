# Publishing the web client

How the published client differs from the one the dev server runs, how it is deployed to the cloud, and how to open it
from another machine or install it as an app. The steps of a first deployment are in
[`AGENTS_FIRST_DEPLOY.md`](../AGENTS_FIRST_DEPLOY.md).

## The published client

`dotnet publish WebClient -c Release` produces the client that players get. It differs from `dotnet build` and the
dev server in one way, set in `WebClient/WebClient.csproj`: the publish is IL-trimmed in full mode. The assemblies
whose members are reached only through reflection are kept whole as `<TrimmerRootAssembly>` entries: the pre-built
serializer, `Metaplay.Client`, `SharedCode.Client`, `WebClientBase` and `WebClient`. Add an entry for a new assembly
the client reaches by reflection. The dev server runs an untrimmed build, so `python3 run_e2e.py --trimmed` is what
tests the trimmed client ([`testing.md`](testing.md#the-e2e-harness)).

No build ships ICU data: `<InvariantGlobalization>` in the same file applies to all of them, so culture-sensitive
formatting uses the invariant culture. A game that formats for the player's locale removes it.

A plain `dotnet build` does not produce a self-contained `wwwroot` (`index.html` and the static assets are resolved
through a manifest, not copied), so serving or deploying the client needs a publish. A Blazor WebAssembly app cannot be
opened from `file://`; serve the published `wwwroot` over HTTP.

## Deploying to the cloud

The game server and the web client deploy separately: the server image does not contain the client.

```bash
metaplay build image
metaplay deploy server <environment> <image>

rm -rf WebClient/bin/Release/net10.0-browser/publish
dotnet publish WebClient -c Release
metaplay deploy webclient <environment>
metaplay deploy webclient <environment> --dry-run   # what would be uploaded, with which headers
```

`metaplay deploy webclient` uploads the published client to the environment's CDN and prints the URL it is served
at. The `webClients` entry in `metaplay-project.yaml` says what is uploaded, with the SDK's `blazor` preset,
`MetaplaySDK/WebClientPresets/blazor.yaml`:

- **A clean publish directory.** A publish never deletes files, so a file removed from `wwwroot`, or one whose name
  changed with its content hash, would otherwise stay in the publish directory and be uploaded.
- **Encoding.** The CDN cannot pick an encoding per request, so the preset uploads the Brotli copy the publish writes
  next to each compressible file, under the file's own name.
- **Cache headers.** Files whose name carries a content hash, the assemblies and runtime files under `_framework/`,
  are cached forever. Everything else is revalidated on each load (`no-cache`): `index.html`, the stylesheets, the
  config archive, and `blazor.webassembly.js` and `dotnet.js`, which the publish does not hash. `dotnet.js` holds the
  boot manifest that names the other framework files, so a new deploy is picked up on the next load.
- **Upload order.** `dotnet.js`, `blazor.webassembly.js` and `index.html` go up last, so a page loaded during a
  deploy finds the files they name.
- **Routes.** The CDN serves files only, with no fallback to `index.html` for a client-side route, so the client
  reloads at the app root (`StartPage.Reload`, [`web-client.md`](web-client.md#the-start-query)).

A player gets a new build on their next page load. If the server refuses a running client as too old, the connection
dialog offers a reload.

## How the client finds its game server

The SDK decides with no code in the client, and `WebClient.Tests/CloudHostingTests.cs` tests it in the browser:

- `?offline` runs with no server ([offline mode](web-client.md#offline-mode)).
- The client reads `metaplay-endpoints.json` next to `index.html`, which `index.html` starts fetching before .NET
  starts. The deploy writes it with the game server's address and the CDN's, as far as the environment reports them,
  in place of the empty one in `WebClient/wwwroot/`. A file with content is used on any page host.
- A page served from `localhost` or an IP address, with the empty file, connects to the game server on the page's
  host, such as one started with `metaplay dev server`. `?wsPort=` and `?cdnPort=` point it at other ports.

For what the file leaves out, the client uses the defaults: WebSocket port 9380, TLS on, and the PublicWebApi at
`<environment>-public.<domain>`.

## Running over the LAN

To open the client on another machine on the same network, such as a phone:

1. Start the game server (`metaplay dev server`). It exposes the client WebSocket on port **9380**.
2. Start the client's dev server (`dotnet run` or `dotnet watch`, [`web-client.md`](web-client.md#running-it)). It
   binds to `0.0.0.0:5290` (`WebClient/Properties/launchSettings.json`), all interfaces.
3. Open `http://<dev machine's LAN address>:5290` on the other machine, for example `http://192.168.x.x:5290`.
   `ipconfig` (Windows) or `ipconfig getifaddr en0` (macOS) shows the address. The dev server builds and serves the
   client, so no publish is needed.

The page is served from an IP address, so the client connects to the game server on that address
(`192.168.x.x:9380`), not to the other machine's own `localhost`.

**Windows Firewall** must allow inbound TCP on both ports, the dev server's (5290) and the game server's WebSocket
(9380). Allow them from an administrator PowerShell, with `Private` for a network classified Private, and `Public` too
for a Public one:

```powershell
New-NetFirewallRule -DisplayName "Metaplay WebClient dev (5290)" -Direction Inbound -Protocol TCP -LocalPort 5290 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "Metaplay game server WS (9380)" -Direction Inbound -Protocol TCP -LocalPort 9380 -Action Allow -Profile Private
```

## Installing as an app

`WebClient/wwwroot/index.html` and `WebClient/wwwroot/manifest.webmanifest` configure the client as an installable
web app: `display: standalone`, theme and background colors, and icons. Chromium-based browsers offer an install
button in the address bar, and the installed app opens in its own window without browser chrome. Installing needs a
secure origin: `localhost` or HTTPS. There is no service worker, so the app needs a network.

The icons are in `WebClient/wwwroot/icons/`. The manifest references them by absolute paths (`/icons/...`), so a
client-side route does not resolve them elsewhere. Restart the dev server after changing them; a running one does not
serve new `wwwroot` files.
