namespace GameServer.Sim;

public sealed class Replay(RecordedRun run)
{
    public RecordedRun Run { get; } = run;
    public long FinalTickNumber { get; private set; }

    public WorldSnapshot ReplayRecordedRun()
    {
        var world = World.CreateFromSnapshot(Run.InitialSnapshot);
        var simulation = new Simulation(world, Run.Seed);
        var eventLogByTick = Run.HostEvents.ToLookup(e => e.TickNumber);
        var commandLogByTick = Run.Log.ToLookup(entry => entry.Tick);

        for (long tick = 0; tick < Run.TickCount; tick++)
        {
            foreach (var hostEvent in eventLogByTick[tick])
                simulation.EnqueueHostEvent(hostEvent);

            foreach (var entry in commandLogByTick[tick])
                simulation.EnqueueCommand(entry.Command);

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
    IReadOnlyList<CommandLogEntry> Log,
    IReadOnlyList<HostEvent> HostEvents
);
