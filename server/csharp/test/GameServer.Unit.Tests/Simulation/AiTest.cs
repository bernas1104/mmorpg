using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

public sealed class AiTest
{
    private const int TicksToRun = 500;
    private const int DrawsToTake = 200;
    private const int Seed = 12345;
    private static readonly TilePosition SpawnPosition = new(10, 10);

    [Fact]
    public void GivenNpcWandering_WhenSimulationTicks_ThenNpcStaysOnWalkableTileEveryTick()
    {
        // Arrange
        var world = TestWorlds.Open();
        var npcId = world.SpawnNpc(SpawnPosition);
        var npc = world.GetEntity(npcId)!;
        var simulation = new Simulation(world, Seed);

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
    public void GivenNpcOnCooldown_WhenThinkRunsBeforeItElapses_ThenNoCommandIsProduced()
    {
        // Arrange
        var world = TestWorlds.Open();
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
        var world = TestWorlds.Open();
        var npcId = world.SpawnNpc(SpawnPosition);
        var test = new TestSimulation(world, Seed);

        // Act
        var log = test.Tick(TicksToRun);

        // Assert
        log.Should().NotContain(
            nameof(MoveResult.OnCooldown),
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
        var world = TestWorlds.Open();
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
        var world = TestWorlds.WalledIn();
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
        var world = TestWorlds.SingleCorridor();
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
        var world = TestWorlds.SingleCorridor();
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
            .Be(MoveResult.TargetOccupied);
        npc.TilePosition.Should().Be(new TilePosition(2, 2));

        Movement.TryMove(world, playerId, Direction.South, currentTick: 0)
            .Should()
            .Be(MoveResult.TargetOccupied);
        world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(2, 1));
    }

    [Fact]
    public void GivenPlayerBlockingAnNpc_WhenSimulationTicks_ThenTheNpcsMoveIsRejectedAsTargetOccupied()
    {
        // Arrange
        var world = TestWorlds.SingleCorridor();
        var npcId = world.SpawnNpc(new TilePosition(2, 2));
        world.SpawnPlayer(new TilePosition(2, 1));
        var test = new TestSimulation(world, Seed);

        // Act
        var log = test.Tick(TicksToRun);

        // Assert
        log.Should().Contain(
            $"move command failed for entity {npcId} with result {MoveResult.TargetOccupied}"
        );
        log.Should().NotContain($"move command succeeded for entity {npcId}");
        world.GetEntity(npcId)!.TilePosition.Should().Be(new TilePosition(2, 2));
    }
}
