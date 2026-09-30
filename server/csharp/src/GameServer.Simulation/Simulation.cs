using GameServer.Simulation.Commands;
using GameServer.Simulation.Enums;

namespace GameServer.Simulation;

public sealed class Simulation(World world)
{
    public World World { get; } = world;
    public long TickNumber { get; private set; } = default;
    private readonly CommandQueue _pending = new();

    public void Tick()
    {
        var batch = _pending.Drain();

        foreach (var command in batch.OrderBy(c => c.Sequence))
        {
            Console.WriteLine($"tick {TickNumber}: received {command}");

            switch (command.Command)
            {
                case MoveCommand moveCommand:
                    var result = Movement.TryMove(
                        World,
                        moveCommand.EntityId,
                        moveCommand.Direction,
                        TickNumber
                    );

                    if (result == MoveResult.Success)
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

    public void Enqueue(Command command) => _pending.Enqueue(command);
}
