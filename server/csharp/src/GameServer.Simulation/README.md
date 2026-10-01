# GameServer.Simulation

Authoritative world state and the rules that change it. No rendering, no networking, no
wall-clock, no entropy of its own. Everything here is a deterministic function of state you
hand it.

Milestone 12 will expand this into the full file map and invariant list. It exists from
Milestone 7 onward because one of the things it records is the kind of note that is only
obvious while it is fresh, and useless once it has been forgotten.

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

**Entities are iterated in ascending `EntityId` order, always.** Use `World.GetAll(kind)`.
.NET explicitly does not guarantee `Dictionary` enumeration order and it does shift as entities
are added and removed, so iterating raw values is correct only by accident and until the next
edit. Because the think pass draws from the shared generator once per entity *in iteration
order*, this ordering decides how many draws happen and in what sequence — it is part of the
simulation's identity, not a formatting choice.

**Commands are the only way to change the world.** Players, network clients and the AI all
produce the same `Command` records onto the same `CommandQueue`, and `Movement.TryMove` is the
sole authority on whether any of them become fact. NPCs in particular have no privileged
movement path; `Ai.Think` is pure and never calls `TryMove`. If you find yourself adding a rule
that special-cases entity kind, the symmetry is broken.

**Tile coordinates are `int`.** Not `float`, not `Vector2`. Floating point can differ subtly
across platforms and JIT settings, and a tile grid does not need it.

---

## The limitation this milestone exists to record

**A recorded run replays correctly only against the same build of this project.**

This will look like a replay bug when Milestone 10 hits it, so it is recorded now while the
reasoning is available.

Player command replay is fully decoupled from the AI. Re-enqueueing a recorded log reproduces
a run exactly, because commands are data and applying them is deterministic.

NPC behaviour is different in kind. A recorded run does not store what the NPCs decided — it
stores what players asked for — and an NPC's decisions are **re-derived by running the AI code
again** against the seed. So the reproduction depends on the AI code consuming the generator
stream in exactly the same way it did live. Adding, removing or reordering NPCs changes how
many draws happen. So does changing `WanderChance`, or adding a single new `rng.Chance(...)`
call to a behaviour. Each of those silently rewrites every subsequent draw, and therefore every
NPC's path, and therefore the final world state.

The root cause is worth stating plainly: **the seed is not the only knob.** The *number and
order of draws* is part of the simulation's identity too. Two runs with identical seeds are only
identical if they are also running identical AI.

The escape hatch, when it is needed, is to record the resolved AI commands in the log alongside
the player commands rather than re-deriving them. That costs log size and gives up nothing
else. It is not built now because Milestone 10 has not decided, and building it early would mean
guessing.

---

## File map

| File | Role |
| --- | --- |
| `Map.cs` | Tile grid; walkability and bounds queries |
| `Entity.cs`, `EntityId.cs`, `Enums/EntityKind.cs` | Identity and kind, separate from mutable entity data |
| `World.cs` | Owns the map and the entity dictionary; allocates ids; deterministic kind-filtered access |
| `Simulation.cs` | Owns the generator; think → drain → apply → advance |
| `Commands/` | `Command` record hierarchy and the sequence-stamping queue |
| `Movement.cs` | `TryMove` — the sole movement authority: bounds, walkability, occupancy, cooldown |
| `Ai.cs` | `Think` — NPC behaviours as pure command producers |
| `Rng.cs` | Hand-rolled xorshift64; seed handling and draw primitives |
| `Direction.cs`, `TilePosition.cs` | Grid primitives |
| `Constants/SimulationConstants.cs` | Tick rate and tunables, with the reasoning beside each number |