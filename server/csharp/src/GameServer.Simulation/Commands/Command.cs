
namespace GameServer.Simulation;

public abstract record Command(EntityId EntityId, CommandKind CommandKind);
