using GameServer.Sim;

namespace GameServer.Server;

public sealed record ClientCommandInput(Command Command) : ClientInput(ClientInputType.Command);
