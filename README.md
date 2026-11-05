# Blazor Starter Project

This is the [Metaplay](https://metaplay.io) starter project for games with a Blazor WebAssembly web client. It
contains a small example game that you replace with your own, and it is built to be developed with an AI coding agent.

It is also the base of Metaplay's Blazor samples, Sticky Paws, Table Stakes and Ninja Time Trial, in
[metaplay-shared/samples](https://github.com/metaplay-shared/samples). Their project structure, shared code, tools and
test harness are this project's, and their documentation refers here for them.

The web client is a regular web app built with C#. A coding agent can build it, run it, and test and inspect it in a
browser, without a game engine editor.

## Start a game with an agent

[`AGENTS_GETTING_STARTED.md`](AGENTS_GETTING_STARTED.md) walks a coding agent from an empty directory to
a running game built on this starter project. To use it, start your agent in an empty directory for the project,
and paste a prompt like this into it:

```text
Build a game using Metaplay.

Find the detailed instructions in:
https://github.com/metaplay-shared/blazor-starter-project/blob/main/AGENTS_GETTING_STARTED.md

Here is the game I want: <description of the game>
```

## Deploy to the cloud with an agent

[`AGENTS_FIRST_DEPLOY.md`](AGENTS_FIRST_DEPLOY.md) walks a coding agent through the first deployment of
the game to Metaplay's cloud. Deploying is optional. When you want to, ask your agent:

```text
Deploy the game to Metaplay's cloud by following AGENTS_FIRST_DEPLOY.md.
```

## What's included

- **Example game**: a small clicker game. The player clicks a button to earn clicks, and spends clicks on
  auto-clickers that earn more clicks over time, including while the player is offline.
- **Web client** (`WebClient/`): a Blazor WebAssembly app for desktop browsers. It has an offline mode that runs without a
  server. `metaplay deploy webclient` uploads it to the cloud environment's CDN.
- **Game config** (`GameConfigSource/`, `tools/GameConfigGen/`): tunable values are CSV files, one file per
  config entry. `tools/GameConfigGen` builds them into the server's and the client's config archives. To
  change a value, edit the CSV file, rebuild the archives, and restart the server.

## Run it

Run these from the repository root, each in its own terminal:

```bash
metaplay dev server
dotnet run --project WebClient/WebClient.csproj      # or `dotnet watch`, to rebuild the client on changes
```

Then open http://localhost:5290. Offline mode needs only the web client: http://localhost:5290/?offline

In Rider, the **Server + Client** configuration under `.run/` starts both. In Visual Studio, so does the
**Server + Client** launch profile in `Game.slnLaunch`, once multi-project launch profiles are enabled (Tools >
Options > Preview Features).

## Make it yours

**Set the web client's name.**

- `SharedCode/GlobalOptions.cs`: `projectName`, which `WebClient/Program.cs` passes to the web client as its
  `appTitle`, the browser tab title and the header text. The first deployment sets it to the portal project's
  name.
- `WebClient/wwwroot/manifest.webmanifest`: `name` and `short_name`, which the browser shows when the game is
  installed as an app.
- `WebClient/wwwroot/index.html`: `<title>`, shown while the client loads.

**Set its look.** `ThemeColors` in `WebClient/Program.cs` sets the colors, and `WebClient/wwwroot/app.css` holds
the game's styles, in plain CSS. [`docs/web-client.md`](docs/web-client.md#styles) says where the rest are.

## Replace the example game

Ask your coding agent to replace the example with your game. The example is marked with comments that start
with `TODO: Example content, replace with your own.` Each comment says what to delete and what to keep. The
marked parts are the player state and actions, the config classes and CSV files, the web client's home
page and client service, the server's player actor, the bot client, and the tests. List them with:

```bash
git grep -n "TODO: Example content"
```

## Documentation

- [`AGENTS.md`](AGENTS.md): the coding agent's guide to building, running and testing, common mistakes,
  and coding conventions.
- [`AGENTS_GETTING_STARTED.md`](AGENTS_GETTING_STARTED.md): the coding agent's steps for creating a new
  game from this starter project.
- [`AGENTS_FIRST_DEPLOY.md`](AGENTS_FIRST_DEPLOY.md): the coding agent's steps for the first deployment to
  Metaplay's cloud.
- [`docs/project-structure.md`](docs/project-structure.md): the projects, and which files a game shares with this
  starter project.
- [`docs/web-client.md`](docs/web-client.md): the web client in detail, including offline mode, rendering and
  connection handling.
- [`docs/testing.md`](docs/testing.md): the test layers, the browser test harness, and how a browser test is written.
- [`docs/publishing.md`](docs/publishing.md): the published client, deploying it to the cloud, opening it from another
  machine on the LAN, and installing it as an app.
- [`GameConfigSource/README.md`](GameConfigSource/README.md): how to write a config CSV file.
- [`docs/design/overview.md`](docs/design/overview.md): the game's design documentation.

## License

Copyright Metaplay. The Blazor starter project is licensed under the [Apache License 2.0](LICENSE). The Metaplay SDK
it builds on is licensed separately, under the Metaplay SDK License. Third-party fonts keep their own licenses, which
are next to them.
