# Design Overview

This is the living, high-level design documentation for the project. It describes **what the game is
and how it is structured** — the intent and the shape of the systems — not the implementation details.

> **Keep these docs current.** Treat them as the source of truth for design decisions. Update them when
> the design changes, and add a new doc under `docs/design/` for each significant feature, linked from
> the [Features](#features) table below.
>
> **Stay high-level.** Describe *what* and *why*, not *how*. Do not duplicate code or game-config
> specifics (field names, action codes, balance numbers, schema versions) here — those live in the code
> and configs and would only drift out of date. Link to the relevant area of the codebase instead.

## The game

_This is currently the starter project's example — there is no game yet._ Replace this section with a
short pitch: the genre, the core loop, and what makes it distinctive. One or two paragraphs.

## Architecture

The project is a [Metaplay SDK](https://metaplay.io) game. At a high level it is split into:

- **Shared game logic** (`SharedCode/`, namespace `Game.Logic`) — deterministic state and rules that
  run identically on client and server. The player's state lives in `PlayerModel`; changes are made
  through `PlayerAction`s. Designer-tunable data lives in the game configs.
- **Server** (`Backend/Server/`) — hosts the authoritative simulation as entity actors (the player
  actor, plus any multiplayer entities the game adds), persistence, and LiveOps.
- **Web client** (`WebClient/` + `WebClientBase/`) — a Blazor WebAssembly client that runs the shared
  logic locally and stays in sync with the server. `WebClientBase/` is the game-agnostic
  half; `WebClient/` is the game-specific UI.
- **LiveOps Dashboard** — the operator UI (the SDK's pre-built dashboard by default; can be customized).

The web client targets desktop browsers and uses the browser viewport as a game screen. The main play area stays fixed while
state changes, with secondary controls occupying reserved space. Screens are laid out to fit the
viewport rather than scroll as a whole; individual collections scroll within their own bounded areas.

For the layout, build/run commands, and the SDK patterns used (actions, models, game configs,
multiplayer entities, the WASM serializer), see `AGENTS.md`.

## Features

| Feature | Status | Design doc |
|---------|--------|-----------|
| Game config & over-the-air publishing | Built | [`game-config.md`](game-config.md) |

Add a row and a `docs/design/<feature>.md` doc per feature.

## Open questions

Track unresolved design decisions here so they are not lost.
