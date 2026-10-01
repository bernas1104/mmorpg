using GameServer.Simulation.Commands;
using GameServer.Simulation.Constants;
using GameServer.Simulation.Enums;
using GameServer.Simulation.Extensions;

namespace GameServer.Simulation;

/// <summary>
/// NPC behaviours. Every behaviour here is a <em>command source</em>, not a privileged actor:
/// it emits the same <see cref="Command"/> a player types into the CLI and has exactly the same
/// authority, no more.
/// </summary>
/// <remarks>
/// The architectural consequence is that <see cref="Think"/> mutates nothing -- it reads world
/// state and the generator and returns intent, leaving <c>Movement.TryMove</c> as the sole
/// authority on whether that intent becomes a fact. Keep it that way. The moment the AI calls
/// TryMove itself, players and NPCs stop sharing one movement path and the milestone's headline
/// claim ("zero NPC-specific movement logic") stops being true.
/// </remarks>
public static class Ai
{
    /// <summary>
    /// Returns the commands this npc wants issued this tick -- zero, or one <see cref="MoveCommand"/>.
    /// </summary>
    /// <param name="world">Authoritative world state. Read only.</param>
    /// <param name="npc">The thinking entity. Read only.</param>
    /// <param name="rng">
    /// The simulation's one generator, passed in rather than reached for. Passing rather than
    /// exposing is the point: it keeps every draw site visible in a signature, so the set of
    /// things that consume the stream is enumerable by reading the code. <c>Think</c> is the only
    /// place AI spends randomness, which is the single seam a future behaviour selector
    /// (chase, flee) has to be careful about.
    /// </param>
    /// <param name="currentTick">Simulation tick this decision is being made for. Never a wall clock.</param>
    public static IEnumerable<Command> Think(World world, Entity npc, Rng rng, long currentTick)
    {
        // NPCs only. A player is driven by its input, and letting this run for players would make
        // their position a function of a draw they never asked for.
        if (npc.Kind != EntityKind.NPC) return [];

        // The gate below is a question about *permission to act*, not a reimplementation of the
        // movement rule. TryMove stays the sole authority: it re-checks the cooldown against the
        // same field and will still say no. Gating early costs nothing and keeps the validate /
        // apply / reject log -- the primary debugging tool -- from being flooded, since cooldown
        // is 10 ticks and an ungated roll would have roughly nine of every ten attempts rejected.
        if (currentTick < npc.NextMoveAllowedTick) return [];

        // Gather the directions worth wanting, then roll for whether to want any of them.
        //
        // Order matters on both halves of that. Candidates are gathered before the roll so a
        // skip costs no draws at all -- drawing when you already know you won't act would consume
        // stream and shift every subsequent draw, making behaviour harder to compare across runs.
        // And DirectionExtensions.All is read rather than casting or relying on declaration
        // order: enum ordering is an implementation detail, and letting it leak in would make the
        // direction a wander picks depend on how the enum happens to be written.
        List<Direction> candidateDirections = [];
        foreach (var direction in DirectionExtensions.All)
        {
            var targetPosition = npc.TilePosition.Step(direction);

            // Walkability is a question asked of the map, not a second copy of the movement rule.
            // Occupancy and cooldown are deliberately NOT filtered here -- those stay exclusively in
            // TryMove -- so the ai is free to want a blocked move and be told no. Filtering them
            // here would be silently correct and would leave the shared rejection path untested.
            if (world.Map.IsWalkable(targetPosition))
                candidateDirections.Add(direction);
        }

        // Load-bearing: with no candidates, NextInt32(0) below would have no value to return.
        if (candidateDirections.Count == 0) return [];

        if (!rng.Chance(SimulationConstants.WanderChance)) return [];

        var chosenDirection = candidateDirections[rng.NextInt32(candidateDirections.Count)];
        return [new MoveCommand(npc.Id, chosenDirection)];
    }
}
