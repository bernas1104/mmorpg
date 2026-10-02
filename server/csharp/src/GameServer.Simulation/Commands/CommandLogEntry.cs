namespace GameServer.Simulation.Commands;

public sealed record CommandLogEntry(long Tick, Command Command);
