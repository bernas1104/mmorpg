using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

namespace GameServer.Unit.Tests.Simulation;

public sealed class SnapshotTest
{
    [Fact]
    public void GivenWorldInAKnownState_WhenSnapshot_ThenTheContentsMatchExpectations()
    {
        // Arrange
        var world = TestWorlds.Open(5, 5);
        var playerId = world.SpawnPlayer(new TilePosition(2, 2));
        var npcId = world.SpawnNpc(new TilePosition(3, 2));
        world.GetEntity(npcId)!.TakeDamage(30);

        // Act
        var snapshot = WorldSnapshot.Of(world);

        // Assert
        snapshot.Map.Width.Should().Be(5);
        snapshot.Map.Height.Should().Be(5);
        snapshot.IdCounter.Should().Be(2);
        snapshot.Entities.Should().HaveCount(2);

        var player = snapshot.Entities.Single(e => e.Id == playerId);
        player.Kind.Should().Be(EntityKind.Player);
        player.TilePosition.Should().Be(new TilePosition(2, 2));
        player.Health.Should().Be(Entity.DefaultMaxHealth);

        var npc = snapshot.Entities.Single(e => e.Id == npcId);
        npc.Kind.Should().Be(EntityKind.Npc);
        npc.Health.Should().Be(Entity.DefaultMaxHealth - 30);
    }

    [Fact]
    public void GivenSnapshot_WhenTheWorldChangesAfterwards_ThenTheSnapshotIsUnchanged()
    {
        // Arrange
        var world = TestWorlds.Open(5, 5);
        var playerId = world.SpawnPlayer(new TilePosition(2, 2));
        var snapshot = WorldSnapshot.Of(world);

        // Act
        world.GetEntity(playerId)!.MoveTo(new TilePosition(1, 1));

        // Assert
        snapshot.Entities.Single(e => e.Id == playerId).TilePosition
            .Should().Be(new TilePosition(2, 2));
    }

    [Fact]
    public void GivenMapSnapshot_WhenConvertedBackToMap_ThenWalkabilityMatchesTheOriginal()
    {
        // Arrange
        var world = TestWorlds.SingleCorridor();
        var map = world.Map;

        // Act
        var restored = map.ToSnapshot().ToMap();

        // Assert
        restored.Width.Should().Be(map.Width);
        restored.Height.Should().Be(map.Height);

        for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
                restored.IsWalkable(new TilePosition(x, y))
                    .Should().Be(map.IsWalkable(new TilePosition(x, y)), $"tile ({x}, {y})");
    }
}
