# AGENTS_FIRST_DEPLOY.md

Instructions for an AI coding agent deploying this project to Metaplay's cloud for the first time. Follow
them only when the user asks to deploy.

Work through the steps in order. Each step ends with a check. Do not move on until the check passes. When
a step needs the user (a decision, or something only they can do in the portal), stop, tell them exactly
what to do, and wait. Follow [`AGENTS.md`](AGENTS.md) throughout.

## 1. Check the prerequisites

- **Docker** must be installed and running. Ask the user before installing it.
- **The Metaplay CLI** must be signed in to the user's account: `metaplay auth whoami` shows it. If it does
  not, sign in as in step 3 of `AGENTS_GETTING_STARTED.md`. It must also be recent
  enough to deploy web clients: if `metaplay deploy webclient --help` fails, run `metaplay update cli`.

**Check:** `docker version` shows both a client and a server, `metaplay auth whoami` shows the user's
account, and `metaplay deploy webclient --help` works.

## 2. Set up the portal project

List the portal projects the user can access:

```bash
metaplay get projects --format=json
```

Show the user the projects by name and ID, and ask which one this game should use. If none fits, ask them
to create one in the Metaplay portal at https://portal.metaplay.dev, then list the projects again.

<!-- TODO: Offer to create a new portal project here, for a user who wants one instead of any of their
existing projects, once the Metaplay CLI can create projects. -->

Set the chosen project's ID as `projectID` in `metaplay-project.yaml` and `projectId` in
`SharedCode/GlobalOptions.cs`, and its name as `projectName` in `SharedCode/GlobalOptions.cs`.

**Check:** both files hold the portal project's ID, `projectName` holds its name, and the unit tests and
the E2E suite pass.

## 3. Create an environment

The user creates an environment in the Metaplay portal, in that project. Then add it to the local project:

```bash
metaplay update project-environments
```

**Check:** the environment is listed in `metaplay-project.yaml`.

## 4. Build the server image

Build it as `AGENTS.md` describes, without an image argument, so that the CLI names and tags the image.

**Check:** the build finishes without errors and prints the image name.

## 5. Deploy the server

```bash
metaplay deploy server <environment> <image>
```

`<image>` is the name the build printed.

The `metaplay-devops` skill covers deployments and what to do when one fails.

**Check:** the command reports a successful deployment.

## 6. Deploy the web client

Publish the web client from a clean directory, and upload it to the environment's CDN:

```bash
rm -rf WebClient/bin/Release/net10.0-browser/publish
dotnet publish WebClient -c Release
metaplay deploy webclient <environment>
```

The deploy also writes the game server's address next to the client, so no client configuration is
needed.

**Check:** the command reports a successful deployment and prints the URL the client is served at.

## 7. Open the game

Give the user the URL the web client deploy printed.

**Check:** the user can play the deployed game from that URL.

## Status of this document

This is an early draft. These parts of the flow are not settled yet, and may change:

- **Project identity.** There is no CLI command yet to create a portal project, so step 2 relies on the
  user creating one in the portal when none of their existing projects fits.
- **Cloud environments.** There is no CLI command yet to create an environment, so step 3 relies on the
  user creating it in the portal.
- **The web client's address.** Step 6 puts the web client at the root of the environment's asset CDN,
  `https://<environment>-assets.<domain>/`.
