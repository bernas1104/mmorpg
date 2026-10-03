namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

using GameServer.Simulation;
using GameServer.Unit.Tests.Support;
public sealed class DeterminismTest
{
    private const int TicksToRun = 500;
    private const int Seed = 12345;
    private static readonly TilePosition SpawnPosition = new(10, 10);
    private static readonly TilePosition[] ThreeNpcSpawns =
    [
        new(5, 5),
        new(8, 8),
        new(14, 14),
    ];

    [Fact]
    public void GivenSameSeed_WhenThinkRunsOverManyTicks_ThenPositionAndCommandHistoriesAreIdentical()
    {
        // Arrange & Act
        var first = ThinkWalk(Seed);
        var second = ThinkWalk(Seed);

        // Assert
        first.Directions.Should().NotBeEmpty();
        first.Positions.Should().Equal(second.Positions);
        first.Directions.Should().Equal(second.Directions);
    }

    [Fact]
    public void GivenSameSeed_WhenTwoSimulationsTick_ThenEveryNpcFollowsTheSamePath()
    {
        // Arrange
        var first = CreateSimulationWithThreeNpcs(Seed);
        var second = CreateSimulationWithThreeNpcs(Seed);

        first.Npcs.Select(npc => npc.TilePosition).Should().Equal(ThreeNpcSpawns);

        // Act
        var firstPaths = TickAndTracePaths(first.Simulation, first.Npcs);
        var secondPaths = TickAndTracePaths(second.Simulation, second.Npcs);

        // Assert
        firstPaths.Should().HaveCount(3);

        for (int i = 0; i < firstPaths.Count; i++)
        {
            firstPaths[i].Should().Contain(
                position => position != ThreeNpcSpawns[i],
                $"npc {i} never moved, so its path proves nothing"
            );

            secondPaths[i].Should().Equal(firstPaths[i], $"npc {i} followed a different path");
        }
    }

    [Fact]
    public void GivenDifferentSeeds_WhenThinkRunsOverManyTicks_ThenPathsDiffer()
    {
        // Arrange & Act
        var first = ThinkWalk(1);
        var second = ThinkWalk(2);

        // Assert
        first.Directions.Should().NotBeEmpty();
        second.Directions.Should().NotBeEmpty();

        second.Positions.Should().NotEqual(first.Positions);
        second.Directions.Should().NotEqual(first.Directions);
    }

    [Fact]
    public void GivenSeedZero_WhenThinkRunsOverManyTicks_ThenTheGeneratorIsNeitherFrozenNorAliased()
    {
        // Arrange & Act
        var zeroSeed = ThinkWalk(seed: 0);

        // Assert
        zeroSeed.Directions.Should().NotBeEmpty(
            "zero is an absorbing state for xorshift: seeding with it as-is emits 0 forever, "
            + "which is exactly reproducible and would sail through the same-seed test"
        );

        zeroSeed.Positions.Should().NotEqual(
            ThinkWalk(seed: 1).Positions,
            "seeding has to map the requested seed onto a non-zero state, so seed 0 cannot end up "
            + "sharing a stream with seed 1"
        );
    }

    private static World ThinkWalkWorld() => TestWorlds.Open();

    private static WalkHistory ThinkWalk(int seed)
    {
        var world = ThinkWalkWorld();
        var npcId = world.SpawnNpc(SpawnPosition);
        var npc = world.GetEntity(npcId)!;
        var rng = new Rng(seed);

        var positions = new List<TilePosition>(TicksToRun);
        var directions = new List<Direction>();

        for (long tick = 0; tick < TicksToRun; tick++)
        {
            foreach (var command in Ai.Think(world, npc, rng, tick))
            {
                var move = Assert.IsType<MoveCommand>(command);

                Movement.TryMove(world, npcId, move.Direction, tick).Should().Be(MoveResult.Success);
                directions.Add(move.Direction);
            }

            positions.Add(npc.TilePosition);
        }

        return new WalkHistory(positions, directions);
    }

    private static (Simulation Simulation, List<Entity> Npcs) CreateSimulationWithThreeNpcs(int seed)
    {
        var world = ThinkWalkWorld();

        foreach (var position in ThreeNpcSpawns) world.SpawnNpc(position);

        return (new Simulation(world, seed), world.GetAliveNpcs().ToList());
    }

    private static List<List<TilePosition>> TickAndTracePaths(
        Simulation simulation,
        List<Entity> npcs
    )
    {
        var paths = npcs.Select(_ => new List<TilePosition>(TicksToRun)).ToList();

        for (int tick = 0; tick < TicksToRun; tick++)
        {
            simulation.Tick();

            for (int i = 0; i < npcs.Count; i++) paths[i].Add(npcs[i].TilePosition);
        }

        return paths;
    }

    private sealed record WalkHistory(List<TilePosition> Positions, List<Direction> Directions);
}
