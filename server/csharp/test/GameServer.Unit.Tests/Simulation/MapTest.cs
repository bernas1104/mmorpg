using GameServer.Simulation;
using GameServer.Unit.Tests.Mocks;

namespace GameServer.Unit.Tests.Simulation;

public sealed class MapTest
{
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void GivenInvalidMapDimensions_WhenCreatingMap_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Map.CreateEmpty(width, height));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 0, false)]
    public void GivenMap_WhenVerifyTileWalkable_ReturnsExpectedResult(int x, int y, bool expected)
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));

        // Act
        var result = map.IsWalkable(new TilePosition(x, y));

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(20, 0, false)]
    [InlineData(0, 20, false)]
    public void GivenMap_WhenVerifyTileWithinBounds_ReturnsExpectedResult(int x, int y, bool expected)
    {
        // Arrange
        var map = Map.FromRows(TileMock.GetWallBoundedTiles(20, 20));

        // Act
        var result = map.IsInBounds(new TilePosition(x, y));

        // Assert
        result.Should().Be(expected);
    }
}
