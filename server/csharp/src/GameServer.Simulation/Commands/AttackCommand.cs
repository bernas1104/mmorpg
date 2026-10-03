namespace GameServer.Simulation;

public sealed record AttackCommand(EntityId AttackerId, EntityId TargetId)
    : Command(CommandPhase.Attack)
{
    public override string ToString() => $"AttackCommand(AttackerId: {AttackerId}, TargetId: {TargetId})";
}
