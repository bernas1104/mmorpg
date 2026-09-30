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

        foreach (var command in batch)
        {
            Console.WriteLine($"tick {TickNumber}: received {command}");

            switch (command)
            {
                case MoveCommand moveCommand:
                    var result = Movement.TryMove(
                        World,
                        moveCommand.EntityId,
                        moveCommand.Direction,
                        TickNumber
                    );

                    if (result == Enums.MoveResult.Success)
                        Console.WriteLine($"tick {TickNumber}: move command succeeded for entity {moveCommand.EntityId}");
                    else
                        Console.WriteLine($"tick {TickNumber}: move command failed for entity {moveCommand.EntityId} with result {result}");

                    break;
                default:
                    throw new InvalidOperationException($"Unhandled command type: {command.GetType()}");
            }
        }

        TickNumber++;
    }

    public void Enqueue(Command command) => _pending.Add(command);
}
