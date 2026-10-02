using System.Diagnostics;

using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Unit.Tests.Collections;

namespace GameServer.Unit.Tests.Simulation;

[Collection(ConsoleOutputCollection.Name)]
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
        var simulation = CreateSimulation();
        simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));

        // Act
        var firstTick = CaptureTick(simulation.Tick);
        var secondTick = CaptureTick(simulation.Tick);

        // Assert
        firstTick.Should().Contain("North");
        secondTick.Should().NotContain("North");
        simulation.TickNumber.Should().Be(2);
    }

    [Fact]
    public void GivenCommandEnqueuedAfterATick_ThenItIsNotProcessedUntilTheNextTick()
    {
        // Arrange
        var simulation = CreateSimulation();

        // Act
        simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));
        var firstTick = CaptureTick(simulation.Tick);

        simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.South));
        var secondTick = CaptureTick(simulation.Tick);

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
        var simulation = CreateSimulation();
        var commands = new MoveCommand[]
        {
            new(new EntityId(2), Direction.North),
            new(new EntityId(0), Direction.North),
            new(new EntityId(1), Direction.North),
        };

        foreach (var command in commands) simulation.Enqueue(command);

        // Act
        var tick = CaptureTick(simulation.Tick);

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

    private static string CaptureTick(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();

        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}
