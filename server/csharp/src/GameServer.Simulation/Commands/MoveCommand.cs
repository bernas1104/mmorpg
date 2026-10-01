using GameServer.Simulation.Enums;

namespace GameServer.Simulation.Commands;

public sealed record MoveCommand(EntityId EntityId, Direction Direction)
    : Command(EntityId, CommandKind.Move)
{
    public override string ToString() => $"MoveCommand(EntityId: {EntityId}, Direction: {Direction})";
}
