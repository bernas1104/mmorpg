namespace GameServer.Simulation;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;
    public long FinalTickNumber { get; private set; }

    public WorldSnapshot ReplayRecordedRun()
    {
        var world = World.CreateFromSnapshot(Run.InitialSnapshot);
        var simulation = new Simulation(world, Run.Seed);
        var logByTick = Run.Log.ToLookup(entry => entry.Tick);

        for (long tick = 0; tick < Run.TickCount; tick++)
        {
            foreach (var entry in logByTick[tick])
                simulation.Enqueue(entry.Command);

            simulation.Tick();
        }

        FinalTickNumber = simulation.TickNumber;

        return WorldSnapshot.Of(world);
    }
}

public sealed record RecordedRun(
    long TickCount,
    int Seed,
    WorldSnapshot InitialSnapshot,
    IReadOnlyList<CommandLogEntry> Log
);
