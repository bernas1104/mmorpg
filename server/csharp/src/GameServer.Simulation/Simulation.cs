using GameServer.Simulation.Commands;
using GameServer.Simulation.Enums;

namespace GameServer.Simulation;

public sealed class Simulation(World world, int seed)
{
    public World World { get; } = world;
    public long TickNumber { get; private set; } = default;
    private readonly CommandQueue _pending = new();

    /// <summary>
    /// The simulation's one and only generator. Created here, at construction, from the seed the
    /// caller supplied -- never inside gameplay code, never <c>Random.Shared</c>, which is
    /// process-global mutable state and would make the simulation's output a function of unrelated
    /// code such as logging or analytics.
    /// </summary>
    /// <remarks>
    /// Exposed read-only because the seed is only half the story: <see cref="Rng.State"/> is the
    /// generator's position in its stream, and a recorded run needs it to be resumed rather than
    /// restarted. Nothing outside the simulation should draw from it; gameplay code receives this
    /// instance as a parameter instead, so that every consumption of the stream is visible in a
    /// signature. The property name intentionally matches the type name -- C#'s "Color Color" rule
    /// makes that unambiguous, and the alternative is a worse name for the same field.
    /// </remarks>
    public Rng Rng { get; } = new(seed);

    public int RngSeed => Rng.Seed;

    /// <summary>
    /// Advances the simulation by one tick: think, drain, apply, then advance the tick number.
    /// Never reads the wall clock and never sleeps -- pacing belongs to the caller.
    /// </summary>
    public void Tick()
    {
        // THINK PASS -- runs at the top of the tick, before the queue is drained, so the commands
        // it produces are part of this tick's batch rather than the next one's. That is not a
        // violation of "a command enqueued mid-tick is processed next tick": this is the top of a
        // tick, not the middle of one.
        //
        // It also fixes the order in which entities draw from the generator -- ascending id, via
        // World.GetAll -- which is what makes "same seed, same run" hold. See the tie-break note
        // below for what that costs.
        foreach (var npc in World.GetAllAliveNPCs())
        {
            var commands = Ai.Think(World, npc, Rng, TickNumber);
            foreach (var command in commands)
                _pending.Enqueue(command);
        }

        var batch = _pending.Drain();

        // APPLY -- deterministic, and the tie-break that falls out of it is worth knowing about.
        //
        // Two sort keys, and the first is the one the milestone plan asks for. Ordering by
        // CommandKind groups the batch into phases -- every move resolves before any attack --
        // so a player who moves and attacks on the same tick attacks from the tile they just
        // moved to. That is the plan's "2. movement, 3. combat" written as one pass instead of
        // two loops. The alternative, a single arrival-ordered pass, is equally deterministic
        // but makes "move then attack" and "attack then move" two different worlds, so the
        // outcome would depend on how a client happened to batch its intents.
        //
        // Sequence is the second key and is arrival order: CommandQueue stamps a monotonically
        // increasing ordinal on enqueue, so among commands of the SAME kind whoever asked first
        // resolves first. Within a single source that is exactly first-in-first-out -- two
        // players' moves land in the order the players sent them and are applied in that order.
        //
        // Note the phase grouping is a primary/secondary key on one sort, not a hard-coded
        // precedence: it says nothing about which entity kind goes first, only that a move
        // precedes an attack. Attacks remain arrival-ordered among themselves, so an NPC that
        // learns to attack will contend with a player's attack on equal terms.
        //
        // Arrival order across sources is not a fairness rule either -- it is a consequence of
        // WHERE the think pass sits. Anything enqueued from outside (player input, network) has
        // already been sitting in the queue by the time the think pass runs, so every player
        // command carries a lower ordinal than every npc command raised this tick. Two entities
        // contesting the same walkable tile on the same tick therefore always resolve
        // player-first -- exercised on every contested tick by AiTest. Among several npcs, the
        // lower EntityId goes first.
        //
        // So: "the player always wins the tile" is not a hard-coded precedence over npc kinds,
        // and there is no knob to flip. It is arrival order over the only timeline these two
        // sources share, and an npc has no arrival time -- it can only think at a tick boundary.
        // Note also that the player's claim is the fresher one: the think pass sees the world as
        // it was at the end of the previous tick, so the player arguably deserves the priority.
        //
        // If npcs ever need to win these contests, no amount of reordering fixes it, because
        // there is no finer interleaving to recover. The answers are gameplay ones -- bumping
        // instead of blocking, a priority attribute, line of sight -- not ordering ones. All of
        // those are still deferred: Milestone 6 settled collision as "no pushing/bumping, no
        // swapping", and nothing since has revisited it.
        foreach (var command in batch.OrderBy(c => c.Command.CommandKind).ThenBy(c => c.Sequence))
        {
            Console.WriteLine($"tick {TickNumber}: received {command}");

            switch (command.Command)
            {
                case MoveCommand moveCommand:
                    ExecuteMovementCommand(moveCommand);
                    break;
                case AttackCommand attackCommand:
                    ExecuteAttackCommand(attackCommand);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled command type: {command.GetType()}");
            }
        }

        foreach (var deadEntity in World.GetAllDead())
            deadEntity.DecrementRemovalTick();

        World.RemoveMarkedEntities();

        TickNumber++;
    }

    public void Enqueue(Command command) => _pending.Enqueue(command);

    private void ExecuteMovementCommand(MoveCommand moveCommand)
    {
        var result = Movement.TryMove(World, moveCommand.EntityId, moveCommand.Direction, TickNumber);

        if (result == MoveResult.Success)
            Console.WriteLine($"Tick {TickNumber}: move command succeeded for entity {moveCommand.EntityId}");
        else
            Console.WriteLine($"Tick {TickNumber}: move command failed for entity {moveCommand.EntityId} with result {result}");
    }

    private void ExecuteAttackCommand(AttackCommand attackCommand)
    {
        var attackResult = Combat.TryAttack(
            World,
            attackCommand.AttackerId,
            attackCommand.TargetId,
            TickNumber
        );

        if (attackResult == AttackResult.Hit)
            Console.WriteLine($"Tick {TickNumber}: attack command succeeded for attacker {attackCommand.AttackerId} on target {attackCommand.TargetId}");
        else
            Console.WriteLine($"Tick {TickNumber}: attack command failed for attacker {attackCommand.AttackerId} on target {attackCommand.TargetId} with result {attackResult}");
    }
}
