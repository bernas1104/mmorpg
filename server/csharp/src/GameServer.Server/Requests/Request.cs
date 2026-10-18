using GameServer.Sim;

namespace GameServer.Server;

public sealed record Request(RequestType Type, EntityId EntityId, TilePosition? TilePosition);

public enum RequestType
{
    Join,
    Leave,
}
