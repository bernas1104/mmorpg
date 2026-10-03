namespace GameServer.Simulation;

public sealed class Entity(EntityId id, EntityKind kind, TilePosition tilePosition)
{
    public const int DefaultMaxHealth = 100;
    public const long DefaultCorpseWindowTicks = 100;

    public EntityId Id { get; } = id;
    public EntityKind Kind { get; } = kind;
    public TilePosition TilePosition { get; private set; } = tilePosition;
    public long NextMoveAllowedTick { get; private set; }
    public int MaxHealth { get; } = DefaultMaxHealth;
    public int Health { get; private set; } = DefaultMaxHealth;
    public long NextAttackAllowedTick { get; private set; }
    public LifecycleState LifecycleState { get; private set; } = LifecycleState.Alive;
    public long? TicksUntilRemoval { get; private set; }

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
            TicksUntilRemoval ??= DefaultCorpseWindowTicks;
        }
    }

    public void UpdateNextAttackAllowedTick(long nextAttackAllowedTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nextAttackAllowedTick, nameof(nextAttackAllowedTick));
        NextAttackAllowedTick = nextAttackAllowedTick;
    }

    public void DecrementTicksUntilRemoval()
    {
        if (!TicksUntilRemoval.HasValue || TicksUntilRemoval <= 0) return;

        TicksUntilRemoval--;

        if (TicksUntilRemoval == 0) LifecycleState = LifecycleState.Removed;
    }

    public static Entity CreateFromSnapshot(EntitySnapshot snapshot)
    {
        var entity = new Entity(
            snapshot.Id,
            snapshot.Kind,
            snapshot.TilePosition
        );

        entity.UpdateNextMoveAllowedTick(snapshot.NextMoveAllowedTick);
        entity.Health = snapshot.Health;
        entity.NextAttackAllowedTick = snapshot.NextAttackAllowedTick;
        entity.LifecycleState = snapshot.LifecycleState;
        entity.TicksUntilRemoval = snapshot.TicksUntilRemoval;

        return entity;
    }
}
