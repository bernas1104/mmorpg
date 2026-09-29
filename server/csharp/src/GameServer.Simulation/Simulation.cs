using GameServer.Simulation.Commands;

namespace GameServer.Simulation;

public sealed class Simulation(World world)
{
    public World World { get; } = world;
    public long TickNumber { get; private set; } = default;
    private List<Command> _pending = [];

    public void Tick()
    {
        var batch = _pending;
        _pending = [];

        foreach (var command in batch) Console.WriteLine($"tick {TickNumber}: received {command}");

        TickNumber++;
    }

    public void Enqueue(Command command) => _pending.Add(command);
}
