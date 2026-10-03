namespace GameServer.Simulation;

public enum AttackResult
{
    Hit,
    InvalidEntity,
    InvalidTarget,
    InvalidOutOfRange,
    InvalidExhaustion,
    InvalidDead,
    // TODO: Implement additional attack results when the combat system is fully developed.
    // Miss,
    // CriticalHit,
    // Blocked,
    // Dodged
}
