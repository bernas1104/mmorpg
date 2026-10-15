namespace GameServer.Server;

public abstract record ClientInput(ClientInputType Type);

public enum ClientInputType
{
    Request,
    Command
}
