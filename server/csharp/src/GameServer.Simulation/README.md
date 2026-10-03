# GameServer.Simulation

Authoritative world state and the rules that change it. No rendering, no networking, no
wall-clock, no entropy of its own. Everything here is a deterministic function of state you
hand it.

---

## The boundary

`GameServer.Simulation` is a class library with **zero package references** — no rendering,
no windowing, no logging framework, nothing. If `dotnet build` of this project alone succeeds
on a machine with no graphics stack, the boundary holds. The two seams a Phase 2 network layer
will plug into are data, not services:

- **Input seam:** `Command` records in (`Simulation.Enqueue`).
- **Output seam:** `WorldSnapshot` records out (`WorldSnapshot.Of(world)`).

The simulation writes log lines to a `TextWriter?` passed at construction — silent by default;
`GameServer.Cli` passes `Console.Out`. No ambient global state of any kind is touched here.

---

## Invariants

These are not style preferences. Each one, violated, produces a run that cannot be reproduced
or explained, and none of them fail loudly when broken.

**Time is tick counts.** Every gameplay timer is a `long` tick number. `DateTime`,
`Stopwatch`, `Environment.TickCount` and friends appear nowhere in this project. `Tick()` never
sleeps and never reads a clock; pacing belongs to the caller. This is what lets a test call
`Tick()` a thousand times in microseconds.

**One generator, owned by `Simulation`, passed in explicitly.** `Simulation.Rng` is created
once from a seed the caller supplies. Gameplay code receives it as a parameter rather than
reaching for a shared instance. There is no `new Random()` and no `Random.Shared` here, and
neither is acceptable: `Random.Shared` is process-global mutable state, so drawing from it
would make this simulation's output a function of unrelated code such as logging or analytics.
The same seed always produces the same stream, on any machine and any .NET version, because the
algorithm is six lines in this repository rather than an implementation detail of the BCL.

**Reading ambient entropy is forbidden here.** If you find yourself wanting a random number
that nobody supplied, the seed is what you are missing. The caller mints it, and
`GameServer.Cli` is the single sanctioned place that does so.

**Entities are iterated in ascending `EntityId` order, always.** Use the `World.GetAll*`
accessors. .NET explicitly does not guarantee `Dictionary` enumeration order and it does shift
as entities are added and removed, so iterating raw values is correct only by accident and
until the next edit. Because the think pass draws from the shared generator once per entity *in
iteration order*, this ordering decides how many draws happen and in what sequence — it is part
of the simulation's identity, not a formatting choice.

**Commands are the only way to change the world.** Players, network clients and the AI all
produce the same `Command` records onto the same `CommandQueue`, and `Movement.TryMove` is the
sole authority on whether any of them become fact. NPCs in particular have no privileged
movement path; `Ai.Think` is pure and never calls `TryMove`. If you find yourself adding a rule
that special-cases entity kind, the symmetry is broken.

**Tile coordinates are `int`.** Not `float`, not `Vector2`. Floating point can differ subtly
across platforms and JIT settings, and a tile grid does not need it.

---

## A tick, end to end

`Simulation.Tick()` runs five phases, in order, every tick:

1. **Think** — `Ai.Think` runs once per alive NPC, in ascending `EntityId` order, and enqueues
   the commands the NPCs want. It runs at the *top* of the tick, so NPC commands join this
   tick's batch, not the next one's.
2. **Drain** — the command queue is emptied into a batch. A command enqueued mid-tick is
   processed on the next tick; the think pass is the top of a tick, not the middle of one.
3. **Sort** — the batch is ordered by `CommandPhase` first (every move resolves before any
   attack, so a move-then-attack in one tick attacks from the post-move tile), then by arrival
   `Sequence`. **The declaration order of `CommandPhase` is load-bearing**: `Move` before
   `Attack` *is* the rule "movement resolves before combat". Reordering the enum silently
   changes tick semantics without failing any test.
4. **Apply** — each command is dispatched: `Movement.TryMove` or `Combat.TryAttack` validate
   against authoritative state and return a result (`MoveResult` / `AttackResult`), which is
   logged. Applied / rejected-invalid / rejected-by-rule is the three-outcome vocabulary of
   Milestone 5; the log line is the primary debugging tool.
5. **Lifecycle** — dead entities' corpse windows tick down; expired corpses are removed; then
   the tick counter advances.

Arrival order across sources is a consequence of phase 1: anything enqueued from outside
(player input, later network) is already in the queue when the think pass runs, so player
commands always carry lower ordinals than NPC commands raised in the same tick — that is why
"the player wins the tile" without being hard-coded anywhere.

## Timers

Every gameplay timer is a `long` tick count, never a wall-clock value:

| Field | Written by | Meaning |
| --- | --- | --- |
| `Entity.NextMoveAllowedTick` | `Movement.TryMove` on success | Movement cooldown gate |
| `Entity.NextAttackAllowedTick` | `Combat.TryAttack` on hit | Attack cooldown gate |
| `Entity.TicksUntilRemoval` | `Entity.TakeDamage` at death; decremented by `Simulation` | Corpse window countdown |

`Entity.TicksUntilRemoval` is a **duration** that counts down each tick — not an absolute tick
number. When it reaches zero the entity is marked `Removed` and swept by
`World.RemoveExpiredCorpses()`.

---

## Snapshots

`WorldSnapshot` is the external representation of world state: a `MapSnapshot` (width, height,
and walkability as a plain string of `.`/`#` rows), the `IdCounter`, and one `EntitySnapshot`
per entity. Everything in it is an immutable record of plain data — nothing holds a live `Map`,
`World`, or `Entity` reference, so a snapshot survives later world changes untouched and
compares structurally (`BeEquivalentTo`, record equality on `MapSnapshot`).

`WorldSnapshot.Of(world)` captures; `World.CreateFromSnapshot(snapshot)` reconstructs — that
pair is what replay is built on.

---

## Replay model and the limitation it records

**A recorded run replays correctly only against the same build of this project.**

This will look like a replay bug when networking hits it, so it is recorded while the reasoning
is available.

Player command replay is fully decoupled from the AI. Re-enqueueing a recorded log reproduces
a run exactly, because commands are data and applying them is deterministic.

NPC behaviour is different in kind. A recorded run does not store what the NPCs decided — it
stores what players asked for — and an NPC's decisions are **re-derived by running the AI code
again** against the seed. So the reproduction depends on the AI code consuming the generator
stream in exactly the same way it did live. Adding, removing or reordering NPCs changes how
many draws happen. So does changing `Ai.WanderChance`, or adding a single new `rng.Chance(...)`
call to a behaviour. Each of those silently rewrites every subsequent draw, and therefore every
NPC's path, and therefore the final world state.

The root cause is worth stating plainly: **the seed is not the only knob.** The *number and
order of draws* is part of the simulation's identity too. Two runs with identical seeds are
only identical if they are also running identical AI.

The escape hatch, when it is needed, is to record the resolved AI commands in the log alongside
the player commands rather than re-deriving them. That costs log size and gives up nothing
else. It is not built now because it would mean guessing at what the network phase needs.

---

## File map

| Path | Role |
| --- | --- |
| `Simulation.cs` | Tick orchestration: think → drain → sort → apply → lifecycle; owns the generator; log sink |
| `World.cs` | Authoritative state container; ID allocation; spawn; deterministic ordered access; snapshot reconstruction |
| `Rng.cs` | Hand-rolled xorshift64; seed handling (zero-seed remap) and draw primitives |
| `SimulationConstants.cs` | `TickDurationMs` (consumed by CLI pacing) |
| `Grid/Map.cs` | Tile grid; walkability and bounds queries; `MapSnapshot` conversion |
| `Grid/Tile.cs` | One tile: walkable or not |
| `Grid/TilePosition.cs` | `int` coordinate pair; `Step` in a direction |
| `Grid/Direction.cs`, `Grid/DirectionExtensions.cs` | The four directions |
| `Entities/Entity.cs` | Mutable entity state: position, health, cooldowns, lifecycle transitions |
| `Entities/EntityId.cs` | Strongly-typed id — a bare `int` is never passed where an id is expected |
| `Entities/EntityKind.cs` | `Player` / `Npc` |
| `Entities/LifecycleState.cs` | `Alive` → `Dead` → `Removed` |
| `Commands/Command.cs` | Abstract base: carries the resolution `Phase` |
| `Commands/CommandPhase.cs` | Resolution-phase sort key — declaration order is load-bearing |
| `Commands/MoveCommand.cs`, `Commands/AttackCommand.cs` | The two intents |
| `Commands/CommandQueue.cs` | Per-tick queue; stamps monotonic arrival `Sequence` (+ `QueuedCommand`) |
| `Commands/CommandLogEntry.cs` | `(Tick, Command)` — the replay log entry |
| `Movement/Movement.cs` | `TryMove` — the sole movement authority: bounds, walkability, occupancy, cooldown |
| `Movement/MoveResult.cs` | Movement outcome vocabulary |
| `Combat/Combat.cs` | `TryAttack` — Chebyshev range, damage, attack cooldown |
| `Combat/AttackResult.cs` | Combat outcome vocabulary |
| `Ai/Ai.cs` | `Think` — NPC behaviours as pure command producers; owns `WanderChance` |
| `Replay/Replay.cs` | `Replay.Run` — re-executes a `RecordedRun` (+ `RecordedRun`) |
| `Snapshots/WorldSnapshot.cs` | The output seam record; `Of(world)` captures |
| `Snapshots/MapSnapshot.cs` | Immutable map representation |
| `Snapshots/EntitySnapshot.cs` | Per-entity plain data |

Folders group by domain concept; the whole project is one namespace, `GameServer.Simulation`.
Folders exist for navigation only — never create a sub-namespace named after a type it
contains, since a child namespace shadows the same-named type in name lookup.

---

## Deliberately postponed

- Map files / serialization of snapshots and commands (networking phase)
- Recording resolved AI commands in the replay log (see the replay limitation above)
- Bumping, pushing, position swapping in collision (Milestone 6 settled "no"; nothing has revisited it)
- Follow/chase/flee AI behaviours and a behaviour selector
- Respawn rules, loot, XP — content on top of a working lifecycle
- Chunks/regions, spatial indexes — revisit only when full-map iteration is a measured problem
- ECS — revisit only if entity-kind complexity actually becomes painful
- Threading — single-threaded tick processing is sufficient at this scale
