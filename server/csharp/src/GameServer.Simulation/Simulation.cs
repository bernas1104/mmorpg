namespace GameServer.Simulation;

public sealed class Simulation(World world)
{
    public World World { get; } = world;
    public long TickNumber { get; private set; } = default;

    public void Tick() => TickNumber++;
}
