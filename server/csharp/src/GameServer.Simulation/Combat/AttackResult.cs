namespace GameServer.Simulation;

public enum AttackResult
{
    Hit,
    UnknownEntity,
    SelfTarget,
    TargetOutOfRange,
    OnCooldown,
    EntityDead,
}
