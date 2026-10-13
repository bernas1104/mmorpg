namespace GameServer.Simulation;

public sealed record PlayerSpawned(long TickNumber, EntityId EntityId, TilePosition TilePosition)
    : HostEvent(TickNumber);
