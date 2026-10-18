namespace GameServer.Sim;

public enum AttackResult
{
    Hit,
    UnknownEntity,
    SelfTarget,
    TargetOutOfRange,
    OnCooldown,
    EntityDead,
}
