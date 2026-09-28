namespace GameServer.Simulation;

public sealed class Simulation(World world)
{
    public World World { get; private set; } = world;
    public int TickCount { get; private set; } = default;

    public void Tick() => TickCount++;
}
