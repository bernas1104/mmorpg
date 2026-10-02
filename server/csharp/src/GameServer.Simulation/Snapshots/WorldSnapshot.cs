namespace GameServer.Simulation.Snapshots;

public sealed record WorldSnapshot(
    Map Map,
    int IdCounter,
    IReadOnlyList<EntitySnapshot> Entities
);
