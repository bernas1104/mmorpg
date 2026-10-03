using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

using Simulation = GameServer.Simulation.Simulation;

public sealed class SimulationTest
{
    [Fact]
    public void GivenSimulation_WhenTickedTenTimes_ThenTheTickNumberAdvancesToTen()
    {
        // Arrange
        var simulation = CreateSimulation();

        // Act
        for (int i = 0; i < 10; i++) simulation.Tick();

        // Assert
        simulation.TickNumber.Should().Be(10);
    }

    [Fact]
    public void GivenQueuedCommand_WhenTickedTwice_ThenTheCommandIsProcessedOnlyOnce()
    {
        // Arrange
        var test = CreateTestSimulation();
        test.Simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));

        // Act
        var firstTick = test.Tick();
        var secondTick = test.Tick();

        // Assert
        firstTick.Should().Contain("North");
        secondTick.Should().NotContain("North");
        test.Simulation.TickNumber.Should().Be(2);
    }

    [Fact]
    public void GivenACommandEnqueuedAfterATick_WhenTicked_ThenItIsProcessedOnTheNextTick()
    {
        // Arrange
        var test = CreateTestSimulation();

        // Act
        test.Simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));
        var firstTick = test.Tick();

        test.Simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.South));
        var secondTick = test.Tick();

        // Assert
        firstTick.Should().Contain("North");
        firstTick.Should().NotContain("South");
        secondTick.Should().Contain("South");
        secondTick.Should().NotContain("North");
    }

    [Fact]
    public void GivenMultipleQueuedCommands_WhenTicked_ThenTheyAreProcessedInEnqueueOrder()
    {
        // Arrange
        var test = CreateTestSimulation();
        var commands = new MoveCommand[]
        {
            new(new EntityId(2), Direction.North),
            new(new EntityId(0), Direction.North),
            new(new EntityId(1), Direction.North),
        };

        foreach (var command in commands) test.Simulation.Enqueue(command);

        // Act
        var tick = test.Tick();

        // Assert
        var positions = commands
            .Select(command => tick.IndexOf(command.ToString(), StringComparison.Ordinal))
            .ToArray();

        foreach (var position in positions) position.Should().BeGreaterThanOrEqualTo(0);

        positions[0].Should().BeLessThan(positions[1]);
        positions[1].Should().BeLessThan(positions[2]);
    }

    [Fact]
    public void GivenAttackEnqueuedBeforeAMove_WhenTicked_ThenTheAttackResolvesFromThePostMoveTile()
    {
        // Arrange
        var world = new World(Map.FromRows(TestMaps.GetWallBoundedTiles(10, 10)));
        var playerId = world.SpawnPlayer(new TilePosition(5, 5));
        var npcId = world.SpawnNpc(new TilePosition(5, 7));
        world.GetEntity(npcId)!.UpdateNextMoveAllowedTick(1000);
        var test = new TestSimulation(world, 1);

        // Act -- the attack is enqueued FIRST. If arrival order decided phases, it would resolve
        // from (5,5) and fail on range; the phase sort must move the player before it attacks.
        test.Simulation.Enqueue(new AttackCommand(playerId, npcId));
        test.Simulation.Enqueue(new MoveCommand(playerId, Direction.South));
        test.Tick();

        // Assert
        world.GetEntity(playerId)!.TilePosition.Should().Be(new TilePosition(5, 6));
        world.GetEntity(npcId)!.Health.Should().Be(Entity.DefaultMaxHealth - Combat.AttackDamage);
    }

    private static Simulation CreateSimulation()
        => new(new World(Map.CreateEmpty(8, 8)), 1);

    private static TestSimulation CreateTestSimulation()
        => new(new World(Map.CreateEmpty(8, 8)), 1);
}
