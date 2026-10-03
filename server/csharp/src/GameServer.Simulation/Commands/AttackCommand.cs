
namespace GameServer.Simulation;

public sealed record AttackCommand(EntityId AttackerId, EntityId TargetId)
    : Command(AttackerId, CommandKind.Attack)
{
    public override string ToString() => $"AttackCommand(AttackerId: {AttackerId}, TargetId: {TargetId})";
};
