# Testing

The test layers, the browser test harness, and how a browser test is written. The commands are in
[`AGENTS.md`](../AGENTS.md#tests).

## Test layers

| Layer | Project | Needs |
|---|---|---|
| Shared-code unit tests | `Backend/SharedCode.Tests/` | Nothing |
| Server unit tests | `Backend/Server.Tests/` | Nothing; it starts no server |
| Game config build tests | `tools/GameConfigGen.Tests/` | The sheets and the committed archives |
| Client tests without a browser | `WebClient.Tests/` | Nothing |
| Browser tests (Playwright) | `WebClient.Tests/` | A browser, the web client, and a game server for most tests |

`metaplay test integration` builds and runs the shared-code and server unit tests in the server image, so they may use
only `Backend/` and `SharedCode/`. The config build tests read `GameConfigSource/`, which the image does not contain,
so they live next to their tool.

## Client tests without a browser

The client projects target only the browser, so `WebClient.Tests.csproj` compiles the client files a test needs in
(`<Compile Include="..\WebClient\...">`) and references the SDK's `Metaplay.Client`, which also builds for plain .NET.
Such tests need no server and run with the rest of the suite.

## The E2E harness

`run_e2e.py`, configured by `e2e.toml`, runs the browser tests against a game server and a web client of their own:

- One build of `tools/e2e-stack.slnx` builds the server, the client and the test project. The harness then starts the
  server with a fresh database and the client, on free ports, runs the test phases `e2e.toml` lists, and stops both.
- Free ports keep a run clear of a stack on the default ports, such as one started by hand, and of other checkouts'
  runs. Runs from one checkout share build output, so concurrent runs belong in different checkouts.
- The tests find the stack through `E2E_*` environment variables, which `WebClient.Tests/E2EStack.cs` reads.
- The harness limits how many runs are active on one host at once, and sizes the NUnit worker count from the host's
  cores and the number of active runs: a cold WebAssembly boot starved of CPU fails to start.
- `--trimmed` publishes the client and serves the published files, so the tests run against the trimmed client that
  players get ([`publishing.md`](publishing.md#the-published-client)). CI runs this mode.
- `--serve` leaves the stack running and prints its URL, and `--stop` stops it.
- A phase can set server options for the tests (`testArgs`), such as shorter waits, and list the only options it may
  set (`testArgsAllowed`). `--plain-server` starts the server without them, for playing by hand.

`python3 run_e2e.py --help` documents the options, `e2e.toml` and the environment variables.

## Writing a browser test

A browser fixture derives from `WebClient.Tests/GamePageTest.cs`:

- **Addresses.** Open pages with `ClientUrl(route, query)`, which carries the query that points the client at the
  harness's game server. The client reads that query only when it starts, so a reload after in-app navigation goes to
  a `ClientUrl` again.
- **Checks around the tests.** Once per run, `ClientUnderTest` checks that the web client answering is the one built
  from this working tree. After a test, a passing test fails if Blazor's error bar showed on a page it opened, and a
  failing test prints what the pages' clients logged. Pass a page the test opens besides `Page` through `Watch`, so it
  is checked too.
- **Offline mode.** A test of offline mode calls `ExpectNoGameServerConnections`.
- **Client state.** To check state that a test can't see on screen, don't add it to the UI: add a property to
  `WebClient/Testing/TestState.cs`, set it in `TestStateHook.Get`, and read it with `TestStateAsync` or wait for it
  with `ExpectTestStateAsync`.
- **Locators.** Prefer role and text locators. For a value with no accessible role, add a `data-testid`.

The tests run in parallel, including the tests of one fixture, each on its own fixture instance and browser context,
so each test has its own player (`WebClient.Tests/Parallelism.cs`). Keep shared state static and read-only, and make
a `[OneTimeSetUp]` static. A direct `dotnet test` runs 6 workers; the harness passes its own count.

`PlaywrightBrowserInstall` installs the browser this version of Playwright needs before the first test, so a fresh
checkout needs no install step. `WebClient.Tests/playwright.runsettings` holds the browser settings.

`WebClient.Tests/CloudHostingTests.cs` serves the client under a cloud host name and checks that it connects to the
server its endpoints file names, and only to that
([`publishing.md`](publishing.md#how-the-client-finds-its-game-server)).
