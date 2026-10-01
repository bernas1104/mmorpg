using GameServer.Simulation.Enums;

namespace GameServer.Simulation.Commands;

public abstract record Command(EntityId EntityId, CommandKind CommandKind);
