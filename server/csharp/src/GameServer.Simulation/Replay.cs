using GameServer.Simulation.Commands;

namespace GameServer.Simulation;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;
    private Simulation? _simulation;

    public World ReplayRecordedRun()
    {
        _simulation = new Simulation(Run.World, Run.Seed);

        for (long tick = 0; tick <= Run.LastTick; tick++)
        {
            Console.WriteLine($"Tick {tick}");

            Run.Log
                .Where(entry => entry.Tick == tick)
                .ToList()
                .ForEach(e => _simulation.Enqueue(e.Command));

            _simulation.Tick();
        }

        return _simulation.World;
    }
}

public sealed record RecordedRun(
    long LastTick,
    int Seed,
    World World,
    IReadOnlyList<CommandLogEntry> Log
);
