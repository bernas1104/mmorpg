using System;
using System.IO;
using System.Linq;

using GameServer.Simulation;
using GameServer.Simulation.Commands;

namespace GameServer.Unit.Tests.Simulation;

/// <summary>
/// Covers the Milestone 4 command-queue contract: Tick() drains what was queued,
/// discards it, and never re-processes it.
///
/// These tests assert on captured Console output rather than on any inspection hook.
/// That is deliberate: in Milestone 4 the log line is the only externally visible
/// effect of a command, so the log IS the behaviour under test.
///
/// KNOWN GAP -- mid-tick deferral is not covered here, and cannot be until Milestone 7.
/// Tick() detaches its batch by swapping in a fresh list, which is what makes a command
/// enqueued *during* the drain wait for the next tick. Replacing that swap with
/// copy-then-Clear still passes every test in this file, because nothing calls Enqueue
/// from inside Tick() yet. Verified by deliberately making that change.
///
/// Close this in Milestone 7, when the NPC think-step becomes the first producer that
/// runs mid-tick: assert that commands emitted by Think() during a tick are not drained
/// by that same tick. Do not close it sooner with a test-only Enqueue hook -- that would
/// add a production seam that exists solely for a test.
/// </summary>
public sealed class SimulationCommandTest
{
    [Fact]
    public void GivenQueuedCommand_WhenTickRunsTwice_ThenCommandIsProcessedOnlyOnce()
    {
        // Arrange
        var simulation = CreateSimulation();
        simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));

        // Act -- the second tick is the real assertion: if the drain left the command
        // in the queue, this is where it would show up a second time.
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

        // Assert -- the drain must be a hard boundary, so nothing leaks into the tick
        // that follows it.
        firstTick.Should().Contain("North");
        firstTick.Should().NotContain("South");
        secondTick.Should().Contain("South");
        secondTick.Should().NotContain("North");
    }

    [Fact]
    public void GivenMultipleQueuedCommands_WhenTickRuns_ThenTheyAreProcessedInEnqueueOrder()
    {
        // Arrange -- distinct entity ids, same direction, so each line is identifiable
        var simulation = CreateSimulation();
        simulation.Enqueue(new MoveCommand(new EntityId(0), Direction.North));
        simulation.Enqueue(new MoveCommand(new EntityId(1), Direction.North));
        simulation.Enqueue(new MoveCommand(new EntityId(2), Direction.North));

        // Act
        var tick = CaptureTick(simulation.Tick);

        // Assert
        var positions = new[] { "Value = 0", "Value = 1", "Value = 2" }
            .Select(token => tick.IndexOf(token, StringComparison.Ordinal))
            .ToArray();

        // Guard first: an absent token yields -1, which would otherwise satisfy
        // a plain less-than comparison and let this test pass for the wrong reason.
        foreach (var position in positions) position.Should().BeGreaterThanOrEqualTo(0);

        positions[0].Should().BeLessThan(positions[1]);
        positions[1].Should().BeLessThan(positions[2]);
    }

    // No entity is spawned: in Milestone 4 a command is never applied, so ids are
    // pure labels. Add spawning once Milestone 5 starts resolving them against World.

    private static GameServer.Simulation.Simulation CreateSimulation()
        => new(new World(Map.CreateEmpty(8, 8)));

    /// <summary>
    /// Runs <paramref name="action"/> with Console output redirected and returns what it wrote.
    ///
    /// Console redirection is process-global, so every test using this must live in this
    /// class. xUnit runs tests within a single class sequentially, which is what keeps
    /// this from racing. Putting a console-capturing test in a second class would make
    /// both intermittently observe the other's output.
    /// </summary>
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
