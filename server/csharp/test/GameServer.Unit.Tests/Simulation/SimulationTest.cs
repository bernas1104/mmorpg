using System.Diagnostics;

using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

public sealed class SimulationTest
{
    [Fact]
    public void GivenSimulation_WhenTick_ThenAdvancesSimulation()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var simulation = CreateSimulation();

        // Act
        for (int i = 0; i < 10; i++) simulation.Tick();
        stopwatch.Stop();

        // Assert
        simulation.TickNumber.Should().Be(10);

        stopwatch.ElapsedMilliseconds.Should().BeCloseTo(0, 20);
    }

    [Fact]
    public void GivenQueuedCommand_WhenTickRunsTwice_ThenCommandIsProcessedOnlyOnce()
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
    public void GivenCommandEnqueuedAfterATick_ThenItIsNotProcessedUntilTheNextTick()
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
    public void GivenMultipleQueuedCommands_WhenTickRuns_ThenTheyAreProcessedInEnqueueOrder()
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

    private static GameServer.Simulation.Simulation CreateSimulation()
        => new(new World(Map.CreateEmpty(8, 8)), 1);

    private static TestSimulation CreateTestSimulation()
        => new(new World(Map.CreateEmpty(8, 8)), 1);
}
