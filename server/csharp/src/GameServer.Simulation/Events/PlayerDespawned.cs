namespace GameServer.Simulation;

public sealed record PlayerDespawned(long TickNumber, EntityId EntityId)
    : HostEvent(TickNumber);
