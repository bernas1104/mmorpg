using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class MapTest
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void GivenMap_WhenVerifyTileWalkable_ReturnsExpectedResult(bool isWalkable, bool expected)
    {
        // Arrange
        var map = new Map(1, 1);
        map.Tiles[0, 0].IsWalkable = isWalkable;

        // Act
        var result = map.IsWalkable(0, 0);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public void GivenMap_WhenVerifyTileWithinBounds_ReturnsExpectedResult(int x, int y, bool expected)
    {
        // Arrange
        var map = new Map(1, 1);

        // Act
        var result = map.IsInBounds(x, y);

        // Assert
        Assert.Equal(expected, result);
    }
}
