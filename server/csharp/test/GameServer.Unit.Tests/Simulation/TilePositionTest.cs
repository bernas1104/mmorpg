using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class TilePositionTest
{
    [Theory]
    [InlineData(Directions.North, 0, -1)]
    [InlineData(Directions.South, 0, 1)]
    [InlineData(Directions.East, 1, 0)]
    [InlineData(Directions.West, -1, 0)]
    public void GivenDirection_WhenStepping_ThenTilePositionIsUpdated(Directions direction, int expectedX, int expectedY)
    {
        // Arrange
        var initialPosition = new TilePosition(0, 0);

        // Act
        var newPosition = initialPosition.Step(direction);

        // Assert
        newPosition.X.Should().Be(expectedX);
        newPosition.Y.Should().Be(expectedY);
    }

    [Fact]
    public void GivenTilePosition_WhenStepInvalidDirection_ThenThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var initialPosition = new TilePosition(0, 0);

        // Act && Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => initialPosition.Step((Directions)999));
    }
}
