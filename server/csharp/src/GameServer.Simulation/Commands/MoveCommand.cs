namespace GameServer.Simulation.Commands;

public sealed record MoveCommand(EntityId EntityId, Direction Direction) : Command(EntityId)
{
    public override string ToString() => $"MoveCommand(EntityId: {EntityId}, Direction: {Direction})";
}
