# AGENTS.md

How to develop in this repo. For what the project is and how to adopt it see [`README.md`](README.md); for
SDK concepts and APIs, use the Metaplay docs.

This is the Blazor starter project: a Metaplay game with a Blazor WebAssembly web client, and the base that games
with such a client are built from, including Metaplay's Blazor samples. A game keeps the starter project's shared
files as they are: `WebClientBase/`, the browser test harness and its base classes, and the serializer generator.
[`docs/project-structure.md`](docs/project-structure.md) lists them, with the projects and what each holds.

## Where to look

- [`docs/project-structure.md`](docs/project-structure.md): the projects, and the files games share.
- [`docs/web-client.md`](docs/web-client.md): the web client: offline mode, the start query, the frame loop,
  rendering, the client service, connection handling and styles.
- [`docs/testing.md`](docs/testing.md): the test layers, the E2E harness and how a browser test is written.
- [`docs/publishing.md`](docs/publishing.md): the published client, deploying, the LAN and installing as an app.
- [`docs/design/`](docs/design/overview.md): the game's own design documentation.

Shared game logic lives in `SharedCode/` (namespace `Game.Logic`) and runs deterministically on both
client and server. `SharedCode/` holds only code that both sides run; code only the server runs lives in
`Backend/Server/` (namespace `Game.Server.*`), even when that makes it testable only from `Backend/Server.Tests/`.
Shared code is compiled once for each side, because the client and server SDKs each carry their
own copy of the SDK's core types: `SharedCode/SharedCode.Client.csproj` builds it for the web client
and `Backend/SharedCode/SharedCode.csproj` for the server. Designer-tunable values live in
`GameConfigSource/` as CSV sheets, one per game config entry.

Commands below run from the project root, the directory holding `metaplay-project.yaml`.

The Metaplay SDK lives outside the game's code; do not modify it. Three files say where it is, and they must
agree: `MetaplaySDKPath` in `Directory.Build.props`, which the projects resolve the SDK through;
`sdkRootDir` and `$schema` in `metaplay-project.yaml`, read by the `metaplay` CLI and the server image build;
and the `Metaplay` folder of `Game.slnx`. To move the SDK or use another copy, change all three.

## Build & run

```bash
dotnet build Game.slnx              # build everything: server, web client, tools, tests and the SDK projects

metaplay dev server                 # run the server (add --watch to auto-restart on changes)
metaplay dev botclient              # run the load-testing bot client

# Web client — targets net10.0-browser, which selects the SDK's browser flavor across the graph
dotnet run   --project WebClient/WebClient.csproj
dotnet watch --project WebClient/WebClient.csproj   # the same, rebuilding and reloading on changes
dotnet build WebClient/WebClient.csproj

# Game config. Builds the archive from the CSV sheets in GameConfigSource/ and writes both copies: the
# server's StaticGameConfig.mpa and the client's built-in SharedGameConfig.mpa. A validation error fails the
# build: nothing is written and the command exits non-zero — for a parse failure, which throws, and for the
# SDK's own item validators, which only report into the build report and would otherwise be written into the
# archive. --dry-run is the same build with nothing written, which is the form to run in CI.
dotnet run --project tools/GameConfigGen
dotnet run --project tools/GameConfigGen -- --dry-run

# Read an archive back, to see what a client is actually being served
dotnet run --project tools/GameConfigGen -- --print Backend/Server/GameConfig/StaticGameConfig.mpa

# Cloud: the server image, and the WebClient to the environment's CDN. The webClients entry in
# metaplay-project.yaml says what is uploaded. The publish directory is cleared first: a publish never
# deletes files, so a removed one would otherwise be uploaded again.
metaplay build image
metaplay deploy server <environment> <image>
rm -rf WebClient/bin/Release/net10.0-browser/publish
dotnet publish WebClient -c Release
metaplay deploy webclient <environment>
metaplay deploy webclient <environment> --dry-run   # what would be uploaded, with which headers
```

## Deploying

The first deployment to Metaplay's cloud is optional. Do it only when the user asks, by following
[`AGENTS_FIRST_DEPLOY.md`](AGENTS_FIRST_DEPLOY.md). Later deployments repeat its image build and deploy
steps for a server change, and its web client steps for a client change.

## Tests

**Give a feature unit tests and end-to-end tests**, added or updated in the same change that adds or changes
the feature:

- **Shared-code unit tests** (`Backend/SharedCode.Tests/`, NUnit) — deterministic game logic: actions,
  model state, `GameTick` / fast-forward, and game-config-driven behavior. Fast; no server needed. The game
  config build itself has its own suite next to its tool (`tools/GameConfigGen.Tests/`,
  `GameConfigBuildTests`), because it reads files outside `Backend/`, which the server image's test stage
  does not contain.
- **Server unit tests** (`Backend/Server.Tests/`, NUnit) — logic only the server runs, in `Backend/Server/`. It
  references `Server.csproj` and compiles in `SetupFixture` from `SharedCode.Tests/`; `GuestNamesTests` is the
  example. Also fast; it starts no server. `metaplay test integration` builds and runs it in the
  server image, like `SharedCode.Tests`, so it may use only `Backend/` and `SharedCode/`.
- **End-to-end tests** (`WebClient.Tests/`, Playwright) — player-visible behavior through the real web
  client against a live server: the feature's UI renders, its action runs, and the model + UI update.

Web client logic that needs no browser can get unit tests in `WebClient.Tests/` too, next to the E2E tests.
The client projects target only the browser, so compile the client files in from `WebClient.Tests.csproj`
(`<Compile Include="..\WebClient\...">`), with a reference to the SDK's `Metaplay.Client`, which also
builds for plain .NET. Such tests need no server and run with the rest of the suite.

Copy the auto-clicker example — `PlayerActionTests` (unit) and `ExampleGameTests` (E2E). Unit tests read
tuned values from the committed config through `TestGameConfig`, not from config objects they build, and run
actions and ticks on a player from `PlayerTestHelper`. How a browser test is written, including the fixture base
`GamePageTest`, parallel tests and reading client state through `TestState`, is in
[`docs/testing.md`](docs/testing.md#writing-a-browser-test).

```bash
dotnet test Backend/SharedCode.Tests/SharedCode.Tests.csproj   # unit — run after a shared-code change
dotnet test Backend/Server.Tests/Server.Tests.csproj           # unit — run after a server change, and after a shared-code change it uses
dotnet test tools/GameConfigGen.Tests                           # the config build — run after a sheet or config class change

# The E2E suite installs its Playwright browser itself before the first test; the first run downloads it.

# E2E harness: builds, brings up its own server + web client on free ports, runs the suite, tears it all
# down. Needs Python 3; if `python3` fails (on Windows it can be a Microsoft Store stub), use `python`.
python3 run_e2e.py                                                # the whole suite
python3 run_e2e.py --filter "FullyQualifiedName~SessionTests"     # anything after the flags goes to dotnet test
python3 run_e2e.py --trimmed                                      # the published (trimmed) client, as CI runs it
python3 run_e2e.py --serve                                        # leave the stack up and print its URL...
python3 run_e2e.py --stop                                         # ...and stop it

# Or against a hand-started stack on the default ports:
metaplay dev server                                                           # terminal 1
dotnet run --project WebClient/WebClient.csproj                               # terminal 2 → http://localhost:5290
dotnet test WebClient.Tests/WebClient.Tests.csproj                           # terminal 3
```

**Prefer the harness.** It never collides with another stack's ports (a hand-started one, another
worktree's run, or another project's), so several can run at once. It builds the checkout it runs in, so
run it from a worktree when that checkout's own stack is running by hand. Either way the suite first checks
that the web client answering on the port is the one this tree built, and fails with the reason if not.
`python3 run_e2e.py --help` lists the rest.

## Gotchas

- **Build the WebClient after changing it.** Its `net10.0-browser` target framework is what gives the SDK
  the right transport and compiles out runtime codegen; publish output lands under
  `bin/<config>/net10.0-browser/`. See [`docs/web-client.md`](docs/web-client.md).
- **The WebClient build generates the pre-built WASM serializer** (mono-wasm cannot generate one at
  runtime): it runs `tools/SerializerGen` into `WebClient/obj/`, so a change to `[MetaSerializable]` types
  needs only a WebClient rebuild. Nothing is committed.
- **Add an EF Core migration when the database model changes** — on an SDK upgrade, or when adding or
  changing a persisted entity of the game's own — or the server refuses to start:
  `dotnet ef migrations add <Name> --project Backend/Server/Server.csproj` (e.g. `MetaplayRelease<N>` for an SDK
  upgrade).
- **Rebuild the game-config archive after editing a sheet in `GameConfigSource/` or a config class**:
  `dotnet run --project tools/GameConfigGen`. Commit both copies it writes, so offline mode cannot serve an
  older config than the server. The archives are not byte-reproducible — each build stamps its own version
  hash — so what holds them to the sheets is `GameConfigBuildTests`, which compares the committed archives
  against a fresh build item by item (both halves of the static archive), not a file diff.
- **A new `[GameConfigEntry]` needs a CSV file of the same name in `GameConfigSource/`**, or the build fails
  from inside that entry's own build step. [`GameConfigSource/README.md`](GameConfigSource/README.md) is how
  a sheet is written; the build tool and `tools/GameConfigGen.Tests/` both name the missing file instead.
- **Restart a running web client after building it from elsewhere.** The dev server reads the client's asset list
  once, when it starts, so it cannot serve a client rebuilt underneath it. A build by the running `dotnet watch`
  needs no restart.
- **Stopping `dotnet run` by killing it leaves the app it started running, holding its port.** Free a port by its
  owner (`lsof -ti:5290`) before trusting a run against it. `run_e2e.py` uses free ports and stops what it starts.
- **A persisted entity's table comes from the `[Table]` attribute on its persisted class.** Declaring a `DbSet` for
  it in `GameDbContext` as well fails the migration.
- **Give a new or retyped `PlayerModel` member a fresh `[MetaMember]` id.** Reusing an id with a different
  type, as when replacing the example, makes players already stored fail to load. Their session starts
  crash, and the error appears only in the server log.

## Conventions

- Styles are plain CSS, with no CSS framework. The game's styles go in `WebClient/wwwroot/app.css`; the base
  styles, in `WebClientBase/wwwroot/css/app-base.css`, hold the display font, the reset and the `mp-` classes of
  `WebClientBase`'s components. Take colors from the theme's CSS variables (`--bg-primary`, `--accent-blue`, ...).
  See [`docs/web-client.md`](docs/web-client.md#styles).
- Explicit typing; avoid `var`. Iterate dictionaries with tuples: `foreach ((KeyType k, ValueType v) in dict)`.
- Comments describe the code's current behavior — never narrate the change or prior state.
- Prefer Read / Glob / Grep over `cat` / `find` / `grep`; reserve the shell for real commands.

## Design docs

[`docs/design/`](docs/design/overview.md) is the living design documentation — the source of truth for
what the game is and how its systems fit together. Keep it current and add a doc per significant feature.
Keep it high-level: describe what and why, not how, and don't restate code or game-config specifics.
