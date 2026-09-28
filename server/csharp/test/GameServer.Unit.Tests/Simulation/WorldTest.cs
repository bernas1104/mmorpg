using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class WorldTest
{
    private readonly Faker _faker = new();

    [Fact]
    public void GivenWorld_WhenCreatingWorld_InitializesCorrectly()
    {
        // Arrange
        var map = new Map(512, 512);

        // Act
        var world = new World(map);

        // Assert
        world.Should().NotBeNull();
        world.Map.Should().Be(map);
        world.Entities.Should().BeEmpty();
        world.IdCounter.Should().Be(0);
    }

    [Fact]
    public void GivenWorld_WhenAddingPlayerInValidPosition_ThenPlayerIsAddedCorrectly()
    {
        // Arrange
        var map = new Map(512, 512);
        var world = new World(map);
        var tilePosition = new TilePosition(_faker.Random.Int(0, 511), _faker.Random.Int(0, 511));

        // Act
        var playerId = world.SpawnPlayer(tilePosition);

        // Assert
        playerId.Should().NotBeNull();
        playerId.Value.Should().Be(0);

        var player = world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(tilePosition);
    }

    [Fact]
    public void GivenWorld_WhenSpawningMultiplePlayers_ThenEntityIdsIncrementCorrectly()
    {
        // Arrange
        var map = new Map(512, 512);
        var world = new World(map);
        var tilePosition = new TilePosition(_faker.Random.Int(100, 400), _faker.Random.Int(100, 400));

        // Act && Assert
        for (int i = 0; i < 5; i++)
        {
            var playerId = world.SpawnPlayer(tilePosition);
            playerId.Should().NotBeNull();
            playerId.Value.Should().Be(i);
        }
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(512, 512)]
    [InlineData(-1, 512)]
    [InlineData(512, -1)]
    public void GivenWorld_WhenAddingPlayerInInvalidPosition_ThenThrowsArgumentOutOfRangeException(int x, int y)
    {
        // Arrange
        var map = new Map(512, 512);
        var world = new World(map);
        var invalidTilePosition = new TilePosition(x, y);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => world.SpawnPlayer(invalidTilePosition));
    }

    [Fact]
    public void GivenWorld_WhenGettingNonExistingEntity_ThenReturnsNull()
    {
        // Arrange
        var map = new Map(512, 512);
        var world = new World(map);

        // Act
        var nonExistingEntity = world.GetEntity(new EntityId(_faker.Random.Int(0, 1000)));

        // Assert
        nonExistingEntity.Should().BeNull();
    }
}
