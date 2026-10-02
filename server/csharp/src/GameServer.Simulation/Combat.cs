using GameServer.Simulation.Enums;

namespace GameServer.Simulation;

public static class Combat
{
    public const long AttackCooldownTicks = 40; // 2 seconds assuming 50ms per tick
    public const int AttackDamage = 10;         // Damage dealt per attack - Development only
    public const int AttackRange = 1;           // Range within which an attack can hit - Development only

    public static AttackResult TryAttack(World world, EntityId attackerId, EntityId targetId, long currentTick)
    {
        if (attackerId == targetId) return AttackResult.InvalidTarget;

        var attacker = world.GetEntity(attackerId);
        var target = world.GetEntity(targetId);

        if (attacker == null || target == null) return AttackResult.InvalidEntity;

        if (
            attacker.LifecycleState != LifecycleState.Alive
            || target.LifecycleState != LifecycleState.Alive
        ) return AttackResult.InvalidDead;

        if (!IsWithinAttackRange(attacker, target)) return AttackResult.InvalidOutOfRange;

        if (currentTick < attacker.NextAttackAllowedTick) return AttackResult.InvalidExhaustion;

        target.TakeDamage(AttackDamage);
        attacker.UpdateNextAttackAllowedTick(currentTick + AttackCooldownTicks);

        return AttackResult.Hit;
    }

    /// <summary>
    /// Determines if the target is within the attack range of the attacker.
    /// It uses Chebyshev distance (maximum of the horizontal and vertical distances) to determine
    /// if the target is within range (diagonal distance considered).
    /// </summary>
    /// <param name="attacker">The entity performing the attack.</param>
    /// <param name="target">The entity being targeted.</param>
    /// <returns>True if the target is within attack range; otherwise, false.</returns>
    private static bool IsWithinAttackRange(Entity attacker, Entity target) => Math.Max(
        Math.Abs(attacker.TilePosition.X - target.TilePosition.X),
        Math.Abs(attacker.TilePosition.Y - target.TilePosition.Y)
    ) <= AttackRange;
}
