# AGENTS.md

Polyglot monorepo with three independent parts — each has its own toolchain and is built from its own directory:

- `server/csharp` — the real project: a deterministic, headless, server-authoritative game simulation (.NET 10). See its `README.md` and `src/GameServer.Simulation/README.md` for the full invariants.
- `client/` — Odin + raylib prototype (entrypoint `main.odin`); format with `odinfmt` (tabs, width 100 per `odinfmt.json`).
- `server/go` — placeholder stub, nothing implemented.

The root `package.json` exists only for tooling (husky + commitlint), not for the app.

## Commands

All C# commands run from `server/csharp`:

```sh
dotnet build
dotnet test
dotnet run --project src/GameServer.Cli -- --seed 12345
dotnet test --filter "FullyQualifiedName~<TestName>"   # single test
```

- SDK version is pinned by `global.json` (10.0.401 / `net10.0`).
- Solution file is `GameServer.slnx` (new XML format).
- CI-less repo; `dotnet build` + `dotnet test` is the verification step.

## Commits

Conventional Commits are enforced by a husky `commit-msg` hook via commitlint (`feat:`, `fix:`, `test:`, `refactor:`, `chore:` — see git log for actual usage).

## Gotchas

- `.plans/` is gitignored (`**/.plans/` in the root `.gitignore`), so the roadmap (`server/csharp/.plans/phase-1-plan.md`) and milestone notes exist only on machines that have them. Don't assume other checkouts have them; don't rely on them being in git history.
- `bin/`/`obj/` and `node_modules/` are not all in the root `.gitignore` — some paths are ignored by `server/csharp/.gitignore`; add ignore rules in the right file.

## Simulation determinism invariants (GameServer.Simulation)

These are project rules, not style preferences — breaking them silently breaks reproducibility, and none fail loudly. Enforced by review and tests:

- **No wall-clock in the simulation.** No `DateTime`, `Stopwatch`, `Environment.TickCount`. Time is `long` tick counts at a fixed 20 Hz; pacing lives only in the CLI.
- **No ambient entropy.** No `new Random()`, no `Random.Shared`. The simulation owns one hand-rolled xorshift64 `Rng` created from a caller-supplied seed. Only the CLI mints seeds, and it prints the value it used.
- **Never iterate entities via raw `Dictionary` enumeration.** Use `World.GetAll(kind)` — ascending `EntityId` order. Iteration order decides RNG draw sequence and is part of the simulation's identity.
- **Commands are the only way to change the world.** `Movement.TryMove` is the sole movement authority; `Ai.Think` is pure and only enqueues commands. Never give NPCs a privileged path.
- **Tile coordinates are `int`**, never `float`/`Vector2`.
- **`GameServer.Simulation` has zero package references** — no rendering or networking deps, ever. If `dotnet build` needs a graphics stack, the boundary is broken.
- **Changing AI changes replays.** Adding/removing/reordering NPCs or any RNG draw (e.g. `WanderChance`) rewrites every subsequent RNG draw, so recorded runs only replay against identical simulation code. The seed is not the only knob.

## Repository Rules and Patterns

### Comments

- Don't leave comments to the code unless they add **REAL** value;
- **NEVER** leave comments on tests. Tests should have self explanatory names. Comments should be irrelevant;
- Use `AwesomeAssertions` instead of xUnit's `Assert` for all tests;
