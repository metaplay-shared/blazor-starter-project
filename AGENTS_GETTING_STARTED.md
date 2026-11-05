# AGENTS_GETTING_STARTED.md

Instructions for an AI coding agent that has been asked to build a game with Metaplay.

Work through the steps below in order. Each step ends with a check. Do not move on until the check passes.
When a step needs the user (a browser sign-in, accepting terms, a decision), stop, tell them exactly what
to do, and wait.

These steps create the project in the working directory, which must be empty. If it is not, stop and ask
the user to start you in an empty directory meant for the project.

## 1. Install the tools

The project needs:

- **git**
- **.NET SDK 10.0** or newer: https://dotnet.microsoft.com/download
- **Python 3**, for the end-to-end test harness (`run_e2e.py`)
- **Metaplay CLI** (`metaplay`): follow the instructions at https://github.com/metaplay/cli
- **GitHub CLI** (`gh`), optional, for creating the user's repository from the starter project

Check what is already installed before installing anything. Ask the user before installing system-wide
software.

**Check:** these all succeed: `git --version`, `dotnet --version` (10.0 or newer), `python --version`
(or `python3`), `metaplay version`.

## 2. Get the starter project

Ask the user how they want to start the project, and set it up in the working directory:

- **In a GitHub repository of their own** (recommended for a game they will keep). Create the repository
  from the starter project as a template, private, so the Metaplay SDK can be committed to it later.
  With the GitHub CLI signed in:

  ```bash
  gh repo create <owner>/<name> --private --template metaplay-shared/blazor-starter-project
  git clone https://github.com/<owner>/<name> .
  ```

  Without it, the user clicks **Use this template** on
  https://github.com/metaplay-shared/blazor-starter-project, picks **Private**, and gives you the
  repository's URL to clone.
- **As a copy of the files**, for a fresh project on another host or with no version control yet. Clone the
  starter project into a temporary directory, copy everything except `.git/` into the working directory,
  and delete the temporary clone. Run `git init` if the user wants version control.
- **As a clone of the starter project**, to try it out:

  ```bash
  git clone https://github.com/metaplay-shared/blazor-starter-project .
  git remote rename origin upstream
  ```

  `upstream` keeps the starter project reachable, and leaves `origin` free for the user's own repository.

The first two start with a fresh history. Later changes to the starter project are applied by hand
in any case, once the example game has been replaced. Do not push anywhere unless the user asks.

Install the Metaplay skills into the project, so that agents working on it later load them:

```bash
metaplay skills install --scope project --target standard --target claude
```

This adds the skills under `.agents/skills/` and `.claude/skills/`. Commit them with the project.

Your harness does not reliably load instructions and skills that appear during the session, so load them
yourself and follow them from here on:

- Read [`AGENTS.md`](AGENTS.md) in full.
- Run `metaplay skills try`. It lists the Metaplay skills and how to load each one.

**Check:** `.agents/skills/` and `.claude/skills/` hold the Metaplay skills, you have read `AGENTS.md`, and you
have run `metaplay skills try`.

## 3. Sign in to Metaplay

The SDK is downloaded from the Metaplay portal, which needs an account.

1. Ask the user to create an account at https://portal.metaplay.dev if they do not have one, and to accept
   the privacy policy and terms and conditions there.
2. Run `metaplay auth login --no-browser`, open the URL it prints or give it to the user, and tell them
   to sign in there and send you the code it shows.
3. Run `metaplay auth login --code <code>` with the code they sent.

**Check:** `metaplay auth whoami` shows the user's account.

## 4. Download the Metaplay SDK

Download the version the starter project is built against. Do not pick a newer one: its code, tests
and pre-built files are made for this one.

```bash
metaplay init sdk --sdk-version=39.0
```

If the command says the terms have not been accepted, send the user back to the portal (step 3). It also
has an `--auto-agree` flag that accepts the terms in the user's name: use it only after the user has
explicitly told you to.

Sharing the SDK publicly is not allowed, so never commit `MetaplaySDK/` to a public repository; committing
it to a private one is fine.

**Check:** `sdkVersion` in `MetaplaySDK/version.yaml` starts with `39.0`.

## 5. Build the game

Replace the example game with the user's game. If the user has not described the game yet, ask them
what they want to build. Follow "Replace the example game" in [`README.md`](README.md) and everything in
[`AGENTS.md`](AGENTS.md).

**Check:** the user has played the game and is happy with the first version, and all tests pass.

## 6. Deploy to the cloud (when desired)

Once the check in step 5 passes, offer the deployment once, in passing, for example: "If you want to play
this game outside your own machine, I can deploy it to Metaplay's cloud. It can be done at any point,
finished or not." Do not start a deployment unless the user asks for one.

When they do, follow [`AGENTS_FIRST_DEPLOY.md`](AGENTS_FIRST_DEPLOY.md).
