# Game config sources

Every designer-tunable value in the game, one CSV file per game config entry. The build reads them into a
config archive, and a published archive is the only route any of these numbers takes to a player — no client
build, no server restart. See [`../docs/design/game-config.md`](../docs/design/game-config.md) for the
structure and [`../AGENTS.md`](../AGENTS.md) for the commands.

```bash
dotnet run --project tools/GameConfigGen                            # build both archives
dotnet run --project tools/GameConfigGen -- --dry-run               # build and validate, write nothing
dotnet run --project tools/GameConfigGen -- --print Backend/Server/GameConfig/StaticGameConfig.mpa

# Build a variant somewhere else — a what-if, without touching the committed archives. Every path is taken
# relative to the repo root, wherever the command was run from.
dotnet run --project tools/GameConfigGen \
    -- --sources /tmp/experiment --static /tmp/static.mpa --shared /tmp/shared.mpa
```

**A validation error fails the build.** No archive is written and the command exits non-zero, with the
library, the row and the member named. That holds for both kinds of check: a parse failure throws, and the
SDK's own item validators only report into the build report — the command refuses to write an archive
carrying an error either way. `--dry-run` is the form to run in CI.

## The files

The file name is the entry name declared in `SharedCode/GameConfigs/SharedGameConfig.cs`, and that is not a
convention but the mechanism: the build asks its source for one sheet per declared entry. **Adding an entry
means adding its sheet**, or the build fails — the build tool and the tests in `tools/GameConfigGen.Tests/`
say so in one line rather than leaving it to a stack trace.

| File | Holds |
|---|---|
| `Global.csv` | Game-wide tunables, one per row: currently what a single click is worth |
| `AutoClickers.csv` | The auto-clicker types a player can buy, one per row |

Both are placeholder content for the starter project's example feature. Replace them with the real game's entries.

## How a sheet is written

- **The header row names members.** A column header is the member name, exactly and case-sensitively;
  `displayname` does not match `DisplayName`. An unknown header fails the build — *unless every cell under it
  is empty*, in which case the column is silently dropped, so a typo'd column nobody has filled in yet will
  pass. (A type may instead take its columns from a constructor tagged
  `[MetaGameConfigBuildConstructor]`, matching parameter names case-insensitively. Nothing here uses one;
  `AutoClickerInfo`'s plain constructor is not one.)
- **One column carries the key**, tagged `#key`. A single-object entry such as `Global.csv` is the
  exception: it is written as `Member,Value` rows instead.
- **A blank cell means "not written here"**, which leaves the member at its C# default.
- **A multi-row item** leaves the key cell empty on its continuation rows, and ends at the next row that
  fills one — the key cell alone decides the grouping. A collection member is written as a `Foo[]` column,
  one element per cell down the item's rows (or across the `Value` span in a `Member,Value` sheet); `Foo[N]`
  addresses one element, and a dotted header (`Schedule.Start.Date`) reaches a nested member. A `Foo[]`
  column does not by itself make an item multi-row.
- **`//` in a header cell drops the column; `//` in the leftmost cell drops the row.** That is where a note
  to the next editor goes, and it is how the marker row at the top of each sheet here is ignored. The test is
  the raw cell value, untrimmed, so the `//` must be the first two characters.

The two sheets in this directory are small enough that they exercise only the first three of these. The rest
is written down because the moment a real game config arrives, it needs them.

## Rules worth keeping

- **Ids are permanent; everything else is not.** Player state and analytics reference rows by id, so a row
  can be repriced or renamed but not re-keyed.
- **A published table is never edited in place** once player state can be part-way through it. Retuning
  means authoring a new id and pointing a `Global.csv` row at it, so a player keeps what they were promised.
- **Semantics are code, values are here.** What a currency or an objective *is* is a compatibility contract
  between the server and every stored player state. What these files decide is how much, how many, and which
  one.

## The over-the-air demo

`Global.csv`'s `ClicksPerTap` is the shortest demonstration in the repo: change it, build, publish the
archive from the LiveOps Dashboard on a running server, and the next session's clicks are worth more — no
client build, no restart. Read the result back from the archive the server hands out rather than from the
file it was built from, which is what `--print` is for.

The build tool does not publish. Making an archive active is a Dashboard action, which owns the
authentication and the audit trail for a change that takes effect immediately for every session that
follows.
