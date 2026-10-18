namespace GameServer.Sim;

public readonly record struct EntityId(uint Value)
{
    public override string ToString() => Value.ToString();
}
