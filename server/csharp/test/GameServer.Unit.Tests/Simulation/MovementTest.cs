using GameServer.Simulation;
using GameServer.Simulation.Commands;
using GameServer.Simulation.Enums;

namespace GameServer.Unit.Tests.Simulation;

public sealed class MovementTest
{
    private readonly string[,] _mapRows = new string[,]
    {
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", "#", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", ".", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", "#", "#", "#", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
        { ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", ".", "." },
    };

    private readonly Map _map;
    private readonly World _world;
    private readonly GameServer.Simulation.Simulation _simulation;

    public MovementTest()
    {
        _map = Map.FromRows(_mapRows);
        _world = new(_map);
        _simulation = new(_world);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingNorth_ThenPlayerMovesToExpectedPosition()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(10, 10));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(10, 9);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Theory]
    [InlineData(Direction.North)]
    [InlineData(Direction.South)]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void GivenPlayerAtStartPosition_WhenMovingIntoNonWalkableTile_ThenPlayerDoesNotMove(Direction direction)
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(2, 2));
        var moveCommand = new MoveCommand(playerId, direction);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(2, 2);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingOutOfBounds_ThenPlayerDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(0, 0));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);

        var expectedPosition = new TilePosition(0, 0);

        // Act
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenPlayerAtStartPosition_WhenMovingWithinCooldown_ThenPlayerDoesNotMove()
    {
        // Arrange
        var playerId = _world.SpawnPlayer(new TilePosition(10, 10));
        var moveCommand = new MoveCommand(playerId, Direction.North);

        _simulation.Enqueue(moveCommand);
        _simulation.Tick();

        var expectedPosition = new TilePosition(10, 9);

        // Act
        _simulation.Enqueue(moveCommand);
        _simulation.Tick();

        // Assert
        var player = _world.GetEntity(playerId);
        player.Should().NotBeNull();
        player.TilePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GivenNonExistentPlayer_WhenMoving_ThenReturnsInvalidEntity()
    {
        // Arrange && Act && Assert
        Movement.TryMove(_world, new EntityId(999), Direction.North, 0)
            .Should()
            .Be(MoveResult.InvalidEntity);
    }
}
