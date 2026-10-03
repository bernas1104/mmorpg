using GameServer.Simulation;

using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

public sealed class AiTest
{
    private const int TicksToRun = 500;
    private const int DrawsToTake = 200;
    private const int Seed = 12345;
    private static readonly TilePosition SpawnPosition = new(10, 10);
    private static readonly TilePosition[] ThreeNpcSpawns =
    [
        new(5, 5),
        new(8, 8),
        new(14, 14),
    ];
    private static readonly string[,] SingleCorridorRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", ".", ".", ".", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };
    private static readonly string[,] WalledInRows =
    {
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", ".", "#", "#" },
        { "#", "#", "#", "#", "#" },
        { "#", "#", "#", "#", "#" },
    };

    [Fact]
    public void GivenNpcWandering_WhenSimulationTicks_ThenNpcStaysOnWalkableTileEveryTick()
    {
        // Arrange
        var world = CreateOpenWorld();
        var npcId = world.SpawnNpc(SpawnPosition);
        var npc = world.GetEntity(npcId)!;
        var simulation = new GameServer.Simulation.Simulation(world, Seed);

        // Act
        var moved = false;
        for (int tick = 0; tick < TicksToRun; tick++)
        {
            simulation.Tick();

            world.Map.IsWalkable(npc.TilePosition).Should().BeTrue(
                $"npc stood on a non-walkable tile at tick {tick}: {npc.TilePosition}"
            );

            moved |= npc.TilePosition != SpawnPosition;
        }

        // Assert
        moved.Should().BeTrue(
            "an npc that never moved would satisfy the walkability assertion vacuously"
        );
    }

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

    [Fact]
    public void GivenNpcOnCooldown_WhenThinkRunsBeforeItElapses_ThenNoCommandIsProduced()
    {
        // Arrange
        var world = CreateOpenWorld();
        var npcId = world.SpawnNpc(SpawnPosition);
        var npc = world.GetEntity(npcId)!;
        npc.UpdateNextMoveAllowedTick(Movement.MoveCooldownTicks);
        var rng = new Rng(Seed);

        // Act & Assert
        for (long tick = 1; tick < Movement.MoveCooldownTicks; tick++)
            Ai.Think(world, npc, rng, tick).Should().BeEmpty(
                $"tick {tick} is still inside the cooldown, which ends at tick {npc.NextMoveAllowedTick}"
            );
    }

    [Fact]
    public void GivenNpcWandering_WhenSimulationTicks_ThenNoMoveIsRejectedForCooldown()
    {
        // Arrange
        var world = CreateOpenWorld();
        var npcId = world.SpawnNpc(SpawnPosition);
        var test = new TestSimulation(world, Seed);

        // Act
        var log = test.Tick(TicksToRun);

        // Assert
        log.Should().NotContain(
            nameof(MoveResult.InvalidExhaustion),
            "the think pass gates on NextMoveAllowedTick before emitting anything"
        );

        log.Should().Contain(
            $"move command succeeded for entity {npcId}",
            "the npc should have moved once its cooldown first allowed it"
        );
    }

    [Fact]
    public void GivenPlayerEntity_WhenThinkRuns_ThenNoCommandIsProduced()
    {
        // Arrange
        var world = CreateOpenWorld();
        var playerId = world.SpawnPlayer(SpawnPosition);
        var player = world.GetEntity(playerId)!;
        var rng = new Rng(Seed);

        // Act
        var commands = Enumerable
            .Range(0, DrawsToTake)
            .SelectMany(_ => Ai.Think(world, player, rng, currentTick: 0))
            .ToList();

        // Assert
        commands.Should().BeEmpty("the ai path is npc only; players are driven by their input");
    }

    [Fact]
    public void GivenNpcWalledInOnEverySide_WhenThinkRuns_ThenNoCommandIsProduced()
    {
        // Arrange
        var world = new World(Map.FromRows(WalledInRows));
        var npcId = world.SpawnNpc(new TilePosition(2, 2));
        var npc = world.GetEntity(npcId)!;
        var rng = new Rng(Seed);

        world.Map.IsWalkable(new TilePosition(2, 1)).Should().BeFalse();
        world.Map.IsWalkable(new TilePosition(2, 3)).Should().BeFalse();
        world.Map.IsWalkable(new TilePosition(1, 2)).Should().BeFalse();
        world.Map.IsWalkable(new TilePosition(3, 2)).Should().BeFalse();

        // Act
        var commands = Enumerable
            .Range(0, DrawsToTake)
            .SelectMany(_ => Ai.Think(world, npc, rng, currentTick: 0))
            .ToList();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void GivenNpcWithASingleWalkableNeighbour_WhenThinkRuns_ThenOnlyThatDirectionIsEverChosen()
    {
        // Arrange
        var world = CreateSingleCorridorWorld();
        var npcId = world.SpawnNpc(new TilePosition(2, 2));
        var npc = world.GetEntity(npcId)!;
        var rng = new Rng(Seed);

        // Act
        var commands = Enumerable
            .Range(0, DrawsToTake)
            .SelectMany(_ => Ai.Think(world, npc, rng, currentTick: 0))
            .Cast<MoveCommand>()
            .ToList();

        // Assert
        commands.Should().NotBeEmpty();
        commands.Should().OnlyContain(command => command.Direction == Direction.North);
        commands.Should().OnlyContain(command => command.EntityId == npcId);
    }

    [Fact]
    public void GivenPlayerOnTheOnlyCandidateTile_WhenNpcMoves_ThenItIsRejectedTheSameWayAPlayerWouldBe()
    {
        // Arrange
        var world = CreateSingleCorridorWorld();
        var npcId = world.SpawnNpc(new TilePosition(2, 2));
        var npc = world.GetEntity(npcId)!;
        var playerId = world.SpawnPlayer(new TilePosition(2, 1));
        var rng = new Rng(Seed);

        var commands = Enumerable
            .Range(0, DrawsToTake)
            .SelectMany(_ => Ai.Think(world, npc, rng, currentTick: 0))
            .Cast<MoveCommand>()
            .ToList();

        // Act & Assert
        commands.Should().OnlyContain(command => command.Direction == Direction.North);

        Movement.TryMove(world, npcId, commands[0].Direction, currentTick: 0)
            .Should()
            .Be(MoveResult.TileOccupied);
        npc.TilePosition.Should().Be(new TilePosition(2, 2));

        Movement.TryMove(world, playerId, Direction.South, currentTick: 0)
            .Should()
            .Be(MoveResult.TileOccupied);
        world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(2, 1));
    }

    [Fact]
    public void GivenPlayerBlockingAnNpc_WhenSimulationTicks_ThenTheNpcIsRejectedAsTileOccupied()
    {
        // Arrange
        var world = CreateSingleCorridorWorld();
        var npcId = world.SpawnNpc(new TilePosition(2, 2));
        world.SpawnPlayer(new TilePosition(2, 1));
        var test = new TestSimulation(world, Seed);

        // Act
        var log = test.Tick(TicksToRun);

        // Assert
        log.Should().Contain(
            $"move command failed for entity {npcId} with result {MoveResult.TileOccupied}"
        );
        log.Should().NotContain($"move command succeeded for entity {npcId}");
        world.GetEntity(npcId)!.TilePosition.Should().Be(new TilePosition(2, 2));
    }

    [Fact]
    public void GivenPlayerAndNpcContestingTheSameTile_WhenTheTickIsApplied_ThenThePlayerAlwaysWins()
    {
        const int seeds = 100;

        var contestedTicks = 0;

        for (int seed = 0; seed < seeds; seed++)
        {
            // Arrange
            var world = CreateSingleCorridorWorld();
            var npcId = world.SpawnNpc(new TilePosition(2, 2));
            var playerId = world.SpawnPlayer(new TilePosition(1, 1));
            var test = new TestSimulation(world, seed);

            // Act
            test.Simulation.Enqueue(new MoveCommand(playerId, Direction.East));
            var log = test.Tick(1);

            var npcMoved = log.Contains($"received MoveCommand(EntityId: {npcId}");
            if (!npcMoved) continue;

            // Assert
            contestedTicks++;

            log.Should().Contain(
                $"move command failed for entity {npcId} with result {MoveResult.TileOccupied}",
                "the player holds the tile, so the npc's identical claim is refused by the shared "
                + "collision rule rather than by anything npc-specific"
            );

            world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(2, 1));
            world.GetEntity(npcId)!.TilePosition.Should().Be(new TilePosition(2, 2));
        }

        contestedTicks.Should().BeGreaterThan(
            0,
            "no seed produced a contested tick, so the tie-break rule was never actually exercised"
        );
    }

    private static World CreateOpenWorld() => new(Map.FromRows(TestMaps.GetWallBoundedTiles(20, 20)));

    private static World CreateSingleCorridorWorld() => new(Map.FromRows(SingleCorridorRows));

    private static WalkHistory ThinkWalk(int seed)
    {
        var world = CreateOpenWorld();
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

    private static (GameServer.Simulation.Simulation Simulation, List<Entity> Npcs)
        CreateSimulationWithThreeNpcs(int seed)
    {
        var world = CreateOpenWorld();

        foreach (var position in ThreeNpcSpawns) world.SpawnNpc(position);

        return (new GameServer.Simulation.Simulation(world, seed), world.GetAliveNpcs().ToList());
    }

    private static List<List<TilePosition>> TickAndTracePaths(
        GameServer.Simulation.Simulation simulation,
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
