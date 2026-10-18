namespace GameServer.Sim;

public sealed record PlayerDespawned(long TickNumber, EntityId EntityId)
    : HostEvent(TickNumber);
