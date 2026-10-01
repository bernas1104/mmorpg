using GameServer.Simulation;
using GameServer.Simulation.Enums;
using GameServer.Unit.Tests.Mocks;

namespace GameServer.Unit.Tests.Simulation;

public sealed class WorldTest
{
    private readonly Faker _faker = new();

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
        var tilePosition = new TilePosition(_faker.Random.Int(1, 18), _faker.Random.Int(1, 18));

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
        var tilePosition = new TilePosition(_faker.Random.Int(10, 15), _faker.Random.Int(10, 15));

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
        var nonExistingEntity = world.GetEntity(new EntityId(_faker.Random.Int(0, 1000)));

        // Assert
        nonExistingEntity.Should().BeNull();
    }

    [Fact]
    public void GivenMixedEntities_WhenGettingAllNPCs_ThenReturnsOnlyNpcsInAscendingIdOrder()
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
        var npcs = world.GetAllNPCs().ToList();

        // Assert
        // Ids 0, 2 and 4 by spawn order, with the players interleaved so this also proves the
        // kind filter rather than just the ordering.
        npcs.Select(npc => npc.Id.Value).Should().Equal(0, 2, 4);

        // Ascending id order is the contract, not an implementation detail: the ai think pass
        // draws from the generator once per npc in this order, so the order of this sequence is
        // part of how many draws happen and therefore part of the simulation's identity.
        //
        // Honest caveat on what this pins: today the OrderBy is defensive rather than load-bearing,
        // because Ids come from a monotonic counter and Dictionary enumerates an insertion-only
        // dictionary in insertion order, so dropping the OrderBy would not fail this. What the
        // test does buy is that swapping the backing store, adding entity removal, or handing out
        // ids in any other order now fails loudly instead of silently changing every rng draw
        // that follows.
        npcs.Select(npc => npc.Kind).Should().OnlyContain(kind => kind == EntityKind.NPC);
    }
}
