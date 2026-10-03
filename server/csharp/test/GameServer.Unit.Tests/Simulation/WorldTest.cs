using GameServer.Simulation;
using GameServer.Simulation.Enums;
using GameServer.Unit.Tests.Mocks;

namespace GameServer.Unit.Tests.Simulation;

public sealed class WorldTest
{
    [Fact]
    public void GivenWorld_WhenCreatingWorld_InitializesCorrectly()
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));

        // Act
        var world = new World(map);

        // Assert
        world.Should().NotBeNull();
        world.Map.Should().Be(map);
        world.IdCounter.Should().Be(0);
    }

    [Fact]
    public void GivenWorld_WhenAddingPlayerInValidPosition_ThenPlayerIsAddedCorrectly()
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));
        var world = new World(map);
        var tilePosition = new TilePosition(7, 13);

        // Act
        var playerId = world.SpawnPlayer(tilePosition);

        // Assert
        playerId.Value.Should().Be(0);

        var player = world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(tilePosition);
    }

    [Fact]
    public void GivenWorld_WhenSpawningMultiplePlayers_ThenEntityIdsIncrementCorrectly()
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));
        var world = new World(map);
        var tilePosition = new TilePosition(12, 11);

        // Act && Assert
        for (int i = 0; i < 5; i++)
        {
            var playerId = world.SpawnPlayer(tilePosition);
            playerId.Value.Should().Be(i);
        }
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    [InlineData(19, 19)]
    [InlineData(20, 20)]
    [InlineData(-1, 20)]
    [InlineData(20, -1)]
    public void GivenWorld_WhenAddingPlayerInInvalidPosition_ThenThrowsArgumentOutOfRangeException(int x, int y)
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));
        var world = new World(map);
        var invalidTilePosition = new TilePosition(x, y);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => world.SpawnPlayer(invalidTilePosition));
    }

    [Fact]
    public void GivenWorld_WhenGettingNonExistingEntity_ThenReturnsNull()
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));
        var world = new World(map);

        // Act
        var nonExistingEntity = world.GetEntity(new EntityId(500));

        // Assert
        nonExistingEntity.Should().BeNull();
    }

    [Fact]
    public void GivenMixedEntities_WhenGettingAllAliveNPCs_ThenReturnsOnlyNpcsInAscendingIdOrder()
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));
        var world = new World(map);

        world.SpawnNPC(new TilePosition(3, 3));
        world.SpawnPlayer(new TilePosition(5, 5));
        world.SpawnNPC(new TilePosition(7, 7));
        world.SpawnPlayer(new TilePosition(9, 9));
        world.SpawnNPC(new TilePosition(11, 11));

        // Act
        var npcs = world.GetAllAliveNPCs().ToList();

        // Assert
        npcs.Select(npc => npc.Id.Value).Should().Equal(0, 2, 4);

        npcs.Select(npc => npc.Kind).Should().OnlyContain(kind => kind == EntityKind.NPC);
    }
}
