namespace GameServer.Simulation;

public sealed record MoveCommand(EntityId EntityId, Direction Direction)
    : Command(CommandPhase.Move)
{
    public override string ToString() => $"MoveCommand(EntityId: {EntityId}, Direction: {Direction})";
}
