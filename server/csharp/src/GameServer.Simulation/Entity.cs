using GameServer.Simulation.Enums;

namespace GameServer.Simulation;

public sealed class Entity(EntityId id, EntityKind kind, TilePosition tilePosition)
{
    public EntityId Id { get; } = id;
    public EntityKind Kind { get; } = kind;
    public TilePosition TilePosition { get; private set; } = tilePosition;
    public long NextMoveAllowedTick { get; private set; } = 0;

    public void UpdateNextMoveAllowedTick(long nextMoveAllowedTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nextMoveAllowedTick, nameof(nextMoveAllowedTick));
        NextMoveAllowedTick = nextMoveAllowedTick;
    }

    public void MoveTo(TilePosition tilePosition) => TilePosition = tilePosition;
}
