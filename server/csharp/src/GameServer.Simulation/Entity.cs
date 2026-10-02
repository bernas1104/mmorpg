using GameServer.Simulation.Enums;

namespace GameServer.Simulation;

public sealed class Entity(EntityId id, EntityKind kind, TilePosition tilePosition)
{
    public static int DefaultMaxHealth { get; } = 100;      // Default max health - Development only
    public static long DefaultRemovalTicks { get; } = 100;  // Default removal tick - Development only

    public EntityId Id { get; } = id;
    public EntityKind Kind { get; } = kind;
    public TilePosition TilePosition { get; private set; } = tilePosition;
    public long NextMoveAllowedTick { get; private set; } = 0;
    public int MaxHealth { get; } = DefaultMaxHealth;
    public int Health { get; private set; } = DefaultMaxHealth;
    public long NextAttackAllowedTick { get; private set; } = 0;
    public LifecycleState LifecycleState { get; private set; } = LifecycleState.Alive;
    public long? RemovalTick { get; private set; } = null;

    public void UpdateNextMoveAllowedTick(long nextMoveAllowedTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nextMoveAllowedTick, nameof(nextMoveAllowedTick));
        NextMoveAllowedTick = nextMoveAllowedTick;
    }

    public void MoveTo(TilePosition tilePosition) => TilePosition = tilePosition;

    public void TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");

        Health = Math.Max(Health - damage, 0);

        if (Health == 0)
        {
            LifecycleState = LifecycleState.Dead;
            RemovalTick ??= DefaultRemovalTicks;
        }
    }

    public void UpdateNextAttackAllowedTick(long nextAttackAllowedTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nextAttackAllowedTick, nameof(nextAttackAllowedTick));
        NextAttackAllowedTick = nextAttackAllowedTick;
    }

    public void DecrementRemovalTick()
    {
        if (!RemovalTick.HasValue || RemovalTick <= 0) return;

        RemovalTick--;

        if (RemovalTick == 0) LifecycleState = LifecycleState.Removed;
    }
}
