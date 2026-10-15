# GameServer.Server

The host shell that connects the outside world to the simulation thread. It owns the boundary,
not the rules: it moves commands in and state out, and never re-implements a gameplay rule.

This project references `GameServer.Simulation`. The reverse reference must never exist — the
deterministic core cannot know that threads, sockets, or clocks exist. That direction is the
ownership boundary made structural.

---

## The ownership model

The simulation is single-owner. Everyone else doesn't *call* the sim — they *queue* to it.

- **`Ingress` is the only door.** Producers (CLI stdin today, network sessions tomorrow) push
  `ClientInput`s into a bounded, multi-producer, single-consumer channel. Producers never touch
  the sim and can never stall it: when the channel is full, `TryWrite` drops instead of
  blocking.
- **`GameHost` is the only key-holder.** Only it calls `simulation.Tick()`, and only from the
  thread it was constructed for.
- **`FixedTimestepLoop` is the only clock.** No wall-clock reads anywhere else; the loop's only
  dependency is an injected `TimeProvider`, so tests hand it a fake and pace ticks without
  sleeps.

## A tick, from the outside in

`GameHost.Advance()` is the per-tick *entry* ritual, in order:

1. **Drain ingress** — everything pushed since the last `Advance` becomes this tick's input.
   Draining is the point where many-producer chaos becomes an ordered list.
2. **Admission** — each input is translated: a `ClientCommandInput` is enqueued as a
   `Command` through the same `EnqueueCommand` path a CLI keystroke uses; a `ClientRequest`
   (`Join` / `Leave`) becomes a `HostEvent` (`PlayerSpawned` / `PlayerDespawned`). Join/leave
   are deliberately *not* commands: a command is what a client may ask for, and letting
   clients spawn entities would hand them authority that belongs to the server.
3. **`simulation.Tick()`** — the simulation runs its own five phases; commands admitted at the
   top of the tick were already queued when the think pass ran, so they carry lower ordinals
   than NPC commands raised that tick. "Player beats NPC on a contested tile" survives the new
   path unchanged because it was a consequence of where the think pass sits, not a rule.
4. **Publish** — the per-tick *exit* ritual. A stub today; later: project state → encode once
   → hand to sessions.

**Eligibility** follows from the drain's position: a command is eligible when an `Advance`'s
drain happens after its push. Pushed before `Advance` → applied in that tick. Pushed during
`Tick()` → it missed the drain, so it waits a full tick. Both cases are locked in by tests.

## The guard

`GameHost` asserts, on every `Advance`/`Publish`, that it is running on the owning thread and
that `Advance` is not already in progress. The guard must **fail loudly** (it throws): .NET has
no formal race detector, and this converts "a subtle concurrency mistake that silently corrupts
a replay" into "a red test." It is a safety net for human error, not a design any legitimate
path relies on.

One known sharp edge, recorded before it bites: `FixedTimestepLoop.Start` followed immediately
by `Stop` is a race — the loop thread may observe the cancellation before running a single
iteration. Production use runs the loop long-lived, so this only matters to tests, which pace
the loop through the sleep delegate to stay deterministic.

## Time and pacing

`TickScheduler` owns the accumulator: elapsed time (measured through the injected
`TimeProvider`) is converted into "ticks due" at a fixed 20 Hz (`DefaultTickDurationMs = 50`).
Two rules keep a slow machine honest:

- **Catch-up cap** — a long pause runs at most `MaxCatchUpTicks` (5) ticks, never a hot loop
  re-simulating hours of backlog.
- **Backlog drop** — excess accumulated time beyond the cap is dropped, not chased: this is a
  real-time game, not a batch job.

`FixedTimestepLoop` composes scheduler + host on an explicit dedicated `Thread` (not
thread-pool tasks) — a deliberate statement that "who owns `World`" is decided at construction
and visible in the code, not an emergent property of async scheduling.

---

## File map

| Path | Role |
| --- | --- |
| `Ingress.cs` | The only door: bounded, non-blocking, multi-producer/single-consumer input channel |
| `GameHost.cs` | The only key-holder: drain → admit → `Tick()` → publish; owns the thread/ re-entrancy guard |
| `TickScheduler.cs` | Accumulator over an injected `TimeProvider`; catch-up cap, backlog drop (+ `TickSchedule`) |
| `FixedTimestepLoop.cs` | The only clock: dedicated thread running the scheduler's schedule |
| `Inputs/ClientInput.cs` | Abstract input record + `ClientInputType` discriminator |
| `Inputs/ClientCommand.cs` | A client intent — becomes a `Command` verbatim |
| `Inputs/ClientRequest.cs` | A server decision request — becomes a `HostEvent` |
| `Requests/Request.cs` | `Join` / `Leave` + entity id + position |

One namespace (`GameServer.Server`); folders group by domain concept for navigation only.

---

## Tests

`test/GameServer.Unit.Tests/Server/` locks in the ownership claims:

- **Ingress** — N producers × M commands drain exactly once each, per-producer order preserved;
  a full channel drops without blocking.
- **GameHost** — the two eligibility cases (pushed before `Advance` → this tick; pushed during
  `Tick()` → next tick); host-admitted commands carry lower sequence than same-tick NPC
  commands; join/leave spawn and despawn; both guard failures throw.
- **TickScheduler / FixedTimestepLoop** — fake-clock tests for accumulation, the catch-up cap,
  backlog drop without hot-looping, and per-pass sleep pacing.

---

## Deliberately postponed

- `Publish` is a stub — state projection, encoding, and session delivery come with the
  networking milestones (`GameServer.Network` exists for this).
- Migrating `GameServer.Cli` onto the host shell — the CLI still paces and drives
  `Simulation` directly; the seam is proven in isolation first.
- Transport, sessions, authentication — nothing here knows a socket exists, and it should stay
  that way until the networking phase wires it.
