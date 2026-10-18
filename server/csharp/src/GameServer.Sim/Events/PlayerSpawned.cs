namespace GameServer.Sim;

public sealed record PlayerSpawned(long TickNumber, EntityId EntityId, TilePosition TilePosition)
    : HostEvent(TickNumber);
