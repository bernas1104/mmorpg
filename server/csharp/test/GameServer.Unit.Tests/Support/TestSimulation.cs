namespace GameServer.Unit.Tests.Support;

using GameServer.Simulation;
using Simulation = GameServer.Simulation.Simulation;

/// <summary>
/// Factory for a <see cref="Simulation"/> wired to a private log writer, so tests can assert on
/// the per-tick log without touching the process-global <c>Console.Out</c>.
/// </summary>
public sealed class TestSimulation
{
    private readonly StringWriter _log = new();

    public Simulation Simulation { get; }

    public string Log => _log.ToString();

    public TestSimulation(World world, int seed)
    {
        Simulation = new Simulation(world, seed, _log);
    }

    /// <summary>
    /// Ticks the simulation and returns only the log lines written by these ticks.
    /// </summary>
    public string Tick(int ticks = 1)
    {
        var before = _log.ToString().Length;

        for (var i = 0; i < ticks; i++) Simulation.Tick();

        return _log.ToString()[before..];
    }
}
