namespace GameServer.Simulation;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;

    public WorldSnapshot ReplayRecordedRun()
    {
        var world = World.CreateFromSnapshot(Run.InitialSnapshot);
        var simulation = new Simulation(world, Run.Seed);

        for (long tick = 0; tick <= Run.LastTick; tick++)
        {
            Run.Log
                .Where(entry => entry.Tick == tick)
                .ToList()
                .ForEach(e => simulation.Enqueue(e.Command));

            simulation.Tick();
        }

        var worldSnapshot = WorldSnapshot.Of(world);

        return worldSnapshot;
    }
}

public sealed record RecordedRun(
    long LastTick,
    int Seed,
    WorldSnapshot InitialSnapshot,
    IReadOnlyList<CommandLogEntry> Log
);
