# Game Config and Over-the-Air Publishing

How designer-tunable data is organised, and how a changed value reaches a running game.

## Why this matters more than it looks

**Change a value, publish, and a running game picks it up with no client build and no server restart.** That
is the property the whole structure exists to preserve, and it is worth stating first because it is easy to
lose by accident: a number compiled into the client, or read from a source file at startup, looks identical
in code review and cannot be changed for players who are already playing.

## The decision: config is CSV in the repo

Values are authored as CSV files in [`GameConfigSource/`](../../GameConfigSource/README.md) and built into the
archive by the SDK's own spreadsheet pipeline. One file per entry, named after the entry, read straight from
the working tree.

The reason for files in the repo rather than a Google Sheets integration is that a clean checkout must build:
no credentials, no shared spreadsheet, no external service to set up before the first build succeeds. A
project that outgrows this can point individual entries at Sheets without changing anything else here — the
sheet layout and the config classes are the same either way. The reason for CSV rather than C# is that it is
what a designer edits and what a spreadsheet program opens: keeping the data in a source file would make
every retune a code change and every reviewer a programmer.

Two properties are worth naming, because anything built on top has to keep both:

- **The values travel in the archive, not in a binary.** Nothing here is compiled into the client or the
  server. A running game gets these numbers by downloading a published archive.
- **Nothing game-specific parses the sheets.** There is no custom `GameConfigBuild` in this repo. Naming a
  CSV directory as the build's default source is all the SDK needs to read every declared entry from the file
  of the same name, so what a new feature adds is config *classes*, not a build pipeline.

Values that are *derived* rather than authored stay in code, in the item type's own build constructor. The
test is whether a designer could get it wrong: if there is only one correct value, it is not a column.

## The structure

- **Single-instance tunables** live in one key-value entry (`Global`). A value belongs there when a designer
  could want it different and there is exactly one of it.
- **Collections** live in libraries keyed by a stable id (`AutoClickers`).
- **Ids are permanent.** Player state and analytics reference rows by id, so a row can be repriced but not
  re-keyed.
- **Semantics are code-owned; values are config-owned.** What a currency or an objective *is* is a
  compatibility contract between the server and every stored player state — not a LiveOps knob. What config
  decides is how much, how many, and which one.
- **A published table is not edited in place** once player state can be part-way through it. Retuning
  authors a new id and moves the pointer, so a player keeps what they were promised.

## Both archives are committed, and that is load-bearing

The server boots from an archive on disk, and the web client carries a built-in copy that offline mode has
nothing else to load from. So both are checked in, and one command writes both — a rebuild that wrote only
the server's copy would leave the client running yesterday's content.

The archives are **not** byte-reproducible: every build stamps its own timestamp and version hash, so
identical content produces different files. What holds them to the sheets is therefore a content comparison
rather than a file diff — `GameConfigBuildTests` imports each committed archive and compares it item by item
against a fresh build of the sources, covering the server half as well as the shared one. Without that,
editing a sheet and forgetting to rebuild leaves every test green while the server loads a stale archive.

## Validation is a build failure

A content rule that does not hold must stop the archive from being written, not warn about it. Two things
make that true, and a game adding checks of its own needs both:

- A parse failure or a game check that throws aborts the build.
- **The SDK's own item validators only report.** They write into the build report, the build reports success,
  and the archive is handed back with the error inside it. So the build report is a gate in its own right:
  the tool refuses to write an archive carrying an error-level message, whoever wrote the check.

**A check belongs in the config build wherever the failure it catches would otherwise show up as a
valid-looking screen** — an empty library that reads as a feature switched off, a pointer at a row that was
deleted, a price nothing can afford. Those are the failures nothing at run time reports.

## The publish path

Building an archive and making one active are deliberately separate. The build tool writes bytes; publishing
is a LiveOps Dashboard action.

The split is not incidental. A publish takes effect immediately for every session that follows, so it wants
the things the Dashboard already has and a build tool does not: an authenticated operator, an audit trail,
and a permission that is off on production and staging by default. A tool that published as a side effect of
building would either bypass those or reimplement them badly — and one that publishes only to localhost is a
flag with no real target. So the boundary is that identity and activation belong to the tools that own them,
and the build owns only the archive.

The shortest demonstration of the whole path: change a tunable in a sheet, build, publish from the
Dashboard, and the next session sees the new value with no client build and no restart.
