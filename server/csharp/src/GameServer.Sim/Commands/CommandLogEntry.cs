namespace GameServer.Sim;

public sealed record CommandLogEntry(long Tick, Command Command);
