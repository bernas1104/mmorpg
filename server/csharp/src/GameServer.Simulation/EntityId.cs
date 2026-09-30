namespace GameServer.Simulation;

public readonly record struct EntityId(int Value)
{
    public override string ToString() => Value.ToString();
};
