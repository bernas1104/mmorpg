using System.Diagnostics;

using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class SimulationTest
{
    [Fact]
    public void GivenSimulation_WhenTick_ThenAdvancesSimulation()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var simulation = new GameServer.Simulation.Simulation(new World(new Map(512, 512)));

        // Act
        for (int i = 0; i < 10; i++) simulation.Tick();
        stopwatch.Stop();

        // Assert
        simulation.TickCount.Should().Be(10);

        stopwatch.ElapsedMilliseconds.Should().BeCloseTo(0, 20);
    }
}
