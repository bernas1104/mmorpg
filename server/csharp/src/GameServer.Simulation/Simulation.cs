namespace GameServer.Simulation;

public sealed class Simulation(World world)
{
    public World World { get; private set; } = world;
    public long TickCount { get; private set; } = default;

    public void Tick() => TickCount++;
}
