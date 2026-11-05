# WebClient

The game's Blazor WebAssembly app (`net10.0-browser`). It runs the Metaplay client in the browser, and builds on
`WebClientBase` and `SharedCode.Client` (`../SharedCode/SharedCode.Client.csproj`).

Run these from the project root:

```bash
dotnet run --project WebClient/WebClient.csproj      # dev server on http://localhost:5290; ?offline needs no server
dotnet build WebClient/WebClient.csproj              # also generates the pre-built serializer
dotnet publish WebClient -c Release                  # the trimmed client that `metaplay deploy webclient` uploads
```

[`docs/web-client.md`](../docs/web-client.md) describes the client: the target framework, the serializer, offline
mode, the frame loop, rendering, the client service, connection handling and styles.
[`docs/publishing.md`](../docs/publishing.md) covers the published client, deploying it, the LAN and installing it as
an app.
