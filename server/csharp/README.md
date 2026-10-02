# GameServer — Headless Game Simulation (C#)

A deterministic, server-authoritative, tile-based game simulation that runs with no renderer
and no network — and that you could wrap a network layer around tomorrow without touching its
internals.

This is the C# adaptation of the original Odin-based roadmap. The milestones, concepts, and
teaching goals are unchanged; only the project layout and a handful of language-specific
decisions (command representation, RNG, entity storage) have been reworked to fit C# idioms.

---

## What is here

**Phase 1 goal:** a simulation core whose output is a pure function of the state you hand it.
No wall-clock reads, no ambient entropy, no rendering dependency — enforced by the build, not
by convention.

| Project                      | Role                                                                                                           |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `src/GameServer.Simulation`  | Authoritative world state and the rules that change it. Zero package references.                               |
| `src/GameServer.Cli`         | Headless runner: paces the tick loop, reads commands from stdin, prints tick logs. References Simulation only. |
| `test/GameServer.Unit.Tests` | xUnit suite covering world queries, movement, collision, AI, and the RNG. References Simulation only.          |

The render-independence boundary is a project-reference-level fact: `GameServer.Simulation`
has no `PackageReference` to any graphics library, ever, and the CLI references nothing else.
If `dotnet build` succeeds on a machine with no graphics stack, the boundary is intact.

---

## Build, run, test

Requires the .NET SDK pinned in [`global.json`](global.json) (10.0.401).

```sh
dotnet build
dotnet test
dotnet run --project src/GameServer.Cli -- --seed 12345
```

The CLI reads commands from stdin one per line:

```
move 1 North
move 2 East
```

and prints a line per tick plus any command outcomes. `--seed <n>` overrides the RNG seed;
when omitted, the CLI mints one from fresh entropy and **prints the value it used** at startup
— that print is what makes any run reproducible. `-h` shows the usage.

---

## Status — Phase 1 milestones

Roadmap: [`.plans/phase-1-plan.md`](.plans/phase-1-plan.md). Milestones 0–8 are complete.

| #   | Milestone                                                            | Status |
| --- | -------------------------------------------------------------------- | ------ |
| 0   | Solution skeleton, headless runner, first passing test               | ✅     |
| 1   | Static tile map, walkability + bounds queries                        | ✅     |
| 2   | `Entity`/`EntityId`, `World` container, spawn                        | ✅     |
| 3   | Fixed-timestep `Simulation.Tick()`, decoupled from wall-clock pacing | ✅     |
| 4   | `Command` record hierarchy + sequence-stamped queue                  | ✅     |
| 5   | Movement with validation, cooldown, and applied/rejected outcomes    | ✅     |
| 6   | Multi-entity collision with deterministic conflict resolution        | ✅     |
| 7   | NPCs as command sources: wander AI, hand-rolled xorshift `Rng`       | ✅     |
| 8   | Health, damage, combat range, attack cooldown                        | ✅     |
| 9   | Entity lifecycle: death, removal, respawn                            | ✅     |
| 10  | Determinism pass & replay harness                                    | ✅     |
| 11  | World state snapshot (the networking seam)                           | ⬜     |
| 12  | Test consolidation & `README.md` per project                         | ⬜     |

---

## Architecture

```
Command Sources                    Command Queue
(CLI stdin now — network later) ──▶ (sequence-stamped)
                                   ▲
AI Think Step ─────────────────────┘
(NPCs produce commands too)
                                   │
                                   ▼
        Simulation.Tick()
          1. think pass — NPCs enqueue, in ascending EntityId order
          2. drain the queue into a batch
          3. sort by CommandKind, then by arrival order
          4. validate + apply — all movement (TryMove), then all combat (TryAttack)
          5. advance tick counter
                                   │
                                   ▼
                  Authoritative World State
```

Step 3 is what makes step 4's two phases mean anything. Sorting by kind first means every
movement in a tick resolves before any combat in that tick, so a player who moves and attacks
in the same tick attacks from the tile they just moved to. Sorting by arrival order within a
kind keeps first-in-first-out between two commands of the same kind. The alternative —
a single arrival-ordered pass — is also deterministic, but it makes "move then attack" and
"attack then move" different worlds, which is a property of enqueue order rather than of the
rules.

Time is a fixed 20 Hz tick (`SimulationConstants.TickDurationMs`). `Simulation.Tick()` never
sleeps and never reads a clock — pacing lives entirely in the CLI, which uses a
Stopwatch-based accumulator with a catch-up cap. Every gameplay timer is a `long` tick count.

### The invariants

These are enforced by review and tests, not by the compiler. The full reasoning lives in
[`src/GameServer.Simulation/README.md`](src/GameServer.Simulation/README.md); the short form:

- **Time is tick counts.** No `DateTime`, `Stopwatch`, or `Environment.TickCount` inside the
  simulation. A test can call `Tick()` a thousand times in microseconds.
- **One generator, owned by `Simulation`, passed in explicitly.** A hand-rolled xorshift64
  (not `System.Random` — its seeded sequence is not stable across .NET versions). No
  `new Random()`, no `Random.Shared`: a shared RNG is process-global mutable state, and the
  same seed must produce the same stream forever.
- **Reading ambient entropy is forbidden in the simulation.** The CLI is the single sanctioned
  place a seed is minted, and it always prints the value used.
- **Entities are iterated in ascending `EntityId` order, always.** .NET does not guarantee
  `Dictionary` enumeration order, and the iteration order decides how the RNG stream is
  consumed — it is part of the simulation's identity.
- **Commands are the only way to change the world.** Players, network clients, and AI all
  produce the same `Command` records onto the same queue; NPCs have no privileged movement
  path. `Ai.Think` is pure and never calls `TryMove`.
- **Tile coordinates are `int`.** Floating point can differ across platforms and JIT settings;
  a tile grid does not need it.

### Known limitation, recorded before it bites

A recorded run replays correctly only against the same build of the simulation. Player
command replay is fully decoupled from the AI, but NPC behaviour is re-derived by running the
AI code against the seed — so adding, removing, or reordering NPCs, or changing
`WanderChance`, silently rewrites every subsequent draw and therefore every subsequent path.
**The seed is not the only knob: the number and order of draws is part of the simulation's
identity too.** The escape hatch (recording resolved AI commands in the log) is deliberately
not built yet; see the simulation README for the reasoning.

---

## Deliberately not built

Networking, persistence, authentication, matchmaking, sharding, an ECS, client-side
prediction, and third-party packages beyond the test framework are all postponed by design.
The full list and rationale are in the roadmap's "Decisions to Deliberately Postpone" section.
`Command` in and `WorldSnapshot` out are the two seams a future network layer attaches to;
neither side of that seam exists yet beyond the command queue itself.
