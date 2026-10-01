namespace GameServer.Simulation.Enums;

public enum AttackResult
{
    Hit,
    InvalidEntity,
    InvalidTarget,
    InvalidOutOfRange,
    InvalidExhaustion,
    // TODO: Implement additional attack results when the combat system is fully developed.
    // Miss,
    // CriticalHit,
    // Blocked,
    // Dodged
}
