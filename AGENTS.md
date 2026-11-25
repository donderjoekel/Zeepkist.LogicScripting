# AGENTS.md

## Workspace

- Primary work target: this repo (`Zeepkist.LogicScripting`) — a BepInEx mod for Zeepkist that compiles text Scripts into v18 Logic Blocks in the level editor
- `.scratch/agents-local.md` (untracked), when it exists: where the game, Imui, ZeepSDK and sibling mod sources are on this machine. Read it first

## Read first

The project's knowledge is in the Cronk Project `ZLOGIC` (Cronk MCP tools). Change it there, not in the repo copies.

- Glossary (`read_glossary`) — domain language; use its terms in code, docs and conversation
- Decisions `ZLOGIC/1`–`ZLOGIC/9` (`read_decision`) — the ADRs, same numbers; propose a new one for a new choice
- Document `map-destination-decisions-and-open-quest-bcd8dea8` — destination, decisions so far, open questions
- Documents under `planning-and-research-bc6fa6ba` — how the game's logic system works and what the mod can use, the plan and the Block Manifest names
- Documents under `working-guides-064407d8` — coding standards and in-game tests

The repo copies (`CONTEXT.md`, `docs/adr/`, `CODING_STANDARDS.md`, `.scratch/logic-script/`) date from 2026-11-25. Read them only when you have no Cronk tools, and don't edit them.

`docs/ingame-tests.md` is the exception: `StepListTests` reads its step table, so a change to a Run Step edits that file in the same branch. Make the same change in the Cronk Document `in-game-tests-afe66f64`.

## Build

- `dotnet build Zeepkist.LogicScripting/Zeepkist.LogicScripting.csproj -c Release` → `Zeepkist.LogicScripting/bin/Release/net472/Zeepkist.LogicScripting.dll`
- Game types come from `Zeepkist.GameLibs` (stripped + publicized). The game code is in `Zeepkist.dll`, not `Assembly-CSharp.dll`.

## Trying it in game

- Every ticket ends with an In-Game Test Run. Turn its "try in game" steps (or, for a refactor, the behavior it must keep) into a `.zlstest` Scenario and run it: `tools/ingame-test.sh [--timeout <s>] <scenario>...`. Steam must be running and Zeepkist closed. Each Scenario gets a new level, and a failing one leaves the rest running. Close the game afterwards.
- Where the Scenario lives:
  - one ticket's case → `.scratch/ingame/<issue>-<slug>.zlstest` (untracked, shared by every worktree)
  - a general check of a feature, worth running again later → `Zeepkist.LogicScripting.InGameTests/Scenarios/`
- **Before you report, challenge every hand-test step.** Each check your report would leave to a hand test, or say a Scenario can't do, goes through this, in order:
  1. An existing Run Step (or several together) does it: read the step table in `docs/ingame-tests.md` and `StepList`. Add it to the Scenario and run it again.
  2. A new Run Step could do it, given the mod's driver seams (`ScriptWindow.Driver`, the synthetic keys and mouse): that's a missing Run Step, see below.
  3. Neither: it stays a hand test, and the report says why no step can do it (for example: it needs Unity's own `Input`, which synthetic keys never reach; a real non-US keyboard; or a judgement of how something looks).

  Done when every hand-test step in your report names the reason from 3, and every other check is in a Scenario that passed.
- Report `.ingame-test/<run id>/results.md` and the screenshot paths, alongside the hand-test steps left after that, each with its reason. In the PR body, put the Scenario and its `results.md` in a collapsed `<details>` block.
- **A Run Step is missing** (the Scenario needs something no step can do): build the step separately, not on your branch.
  1. Follow the `impl` skill with a description of the steps as the work (no ticket), on a `test/ingame-<slug>` branch from main. The brief also says the new steps get a repo Scenario that uses them.
  2. Finish the rest of your work, push, and open your PR as a draft (`gh pr create --draft`), saying it waits on that harness branch.
  3. Report "waiting on `test/ingame-<slug>`" with the steps you asked for. Once it's merged you'll be told to merge main, run the Scenario, update the PR and mark it ready.
- `docs/ingame-tests.md` — the step list, adding a step, and what a run changes and puts back.

## Public repo

- The GitHub repo is public; the game source is not. Keep game-internal notes in Cronk Documents under `planning-and-research-bc6fa6ba` or in `.scratch/` (untracked), never in tracked docs (including this file and `CONTEXT.md`) or commit messages. Terms and Decisions can be exported into the repo, so keep them out of there too.

## Code style

- The Cronk Document `coding-standards-c87241ac` for conventions, `.editorconfig` for formatting and naming. Run `dotnet format` before committing; the build should have zero warnings.
