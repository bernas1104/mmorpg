namespace GameServer.Simulation;

public sealed record CommandLogEntry(long Tick, Command Command);
