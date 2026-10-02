using GameServer.Simulation.Commands;
using GameServer.Simulation.Snapshots;

namespace GameServer.Simulation;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;
    private Simulation? _simulation;

    public WorldSnapshot ReplayRecordedRun()
    {
        var world = World.CreateFromSnapshot(Run.World);
        _simulation = new Simulation(world, Run.Seed);

        for (long tick = 0; tick <= Run.LastTick; tick++)
        {
            Console.WriteLine($"Tick {tick}");

            Run.Log
                .Where(entry => entry.Tick == tick)
                .ToList()
                .ForEach(e => _simulation.Enqueue(e.Command));

            _simulation.Tick();
        }

        var worldSnapshot = Snapshot.CreateWorldSnapshot(world);

        return worldSnapshot;
    }
}

public sealed record RecordedRun(
    long LastTick,
    int Seed,
    WorldSnapshot World,
    IReadOnlyList<CommandLogEntry> Log
);
