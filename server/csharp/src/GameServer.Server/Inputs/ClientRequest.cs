namespace GameServer.Server;

public sealed record ClientRequest(Request Request) : ClientInput(ClientInputType.Request);
