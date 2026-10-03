
namespace GameServer.Simulation;

public sealed record MoveCommand(EntityId EntityId, Direction Direction)
    : Command(EntityId, CommandKind.Move)
{
    public override string ToString() => $"MoveCommand(EntityId: {EntityId}, Direction: {Direction})";
}
