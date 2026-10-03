using GameServer.Simulation.Commands;
using GameServer.Simulation.Snapshots;

namespace GameServer.Simulation;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;

    public WorldSnapshot ReplayRecordedRun()
    {
        var world = World.CreateFromSnapshot(Run.World);
        var simulation = new Simulation(world, Run.Seed);

        for (long tick = 0; tick <= Run.LastTick; tick++)
        {
            Run.Log
                .Where(entry => entry.Tick == tick)
                .ToList()
                .ForEach(e => simulation.Enqueue(e.Command));

            simulation.Tick();
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
