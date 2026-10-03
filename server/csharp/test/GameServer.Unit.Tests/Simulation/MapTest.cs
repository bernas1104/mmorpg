using GameServer.Simulation;
using GameServer.Unit.Tests.Support;

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
    [InlineData(25, 1)]  // correct total length, wrong shape
    [InlineData(4, 1)]   // too small
    [InlineData(10, 10)] // too large
    public void GivenTileGridNotMatchingMapDimensions_WhenCreatingMap_ThrowsArgumentException(
        int tileWidth,
        int tileHeight)
    {
        // Arrange
        var tiles = new Tile[tileWidth, tileHeight];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Map.FromTiles(5, 5, tiles));
    }

    [Fact]
    public void GivenTileGridMatchingMapDimensions_WhenCreatingMap_ThenMapUsesThoseDimensions()
    {
        // Arrange
        var tiles = new Tile[3, 2];

        // Act
        var map = Map.FromTiles(3, 2, tiles);

        // Assert
        map.Width.Should().Be(3);
        map.Height.Should().Be(2);
        map.IsInBounds(new TilePosition(2, 1)).Should().BeTrue();
        map.IsInBounds(new TilePosition(3, 0)).Should().BeFalse();
    }

    [Fact]
    public void GivenNonSquareRows_WhenCreatingMap_ThenRowsMapToWidthThenHeight()
    {
        // Arrange
        string[,] rows = new string[,]
        {
            { ".", ".", "." },
            { ".", "#", "." }
        };

        // Act
        var map = Map.FromRows(rows);

        // Assert
        map.Width.Should().Be(3);
        map.Height.Should().Be(2);
        map.IsWalkable(new TilePosition(0, 0)).Should().BeTrue();
        map.IsWalkable(new TilePosition(1, 1)).Should().BeFalse();
        map.IsWalkable(new TilePosition(2, 1)).Should().BeTrue();
    }

    [Theory]
    [InlineData(".", "X")]
    [InlineData("X", ".")]
    public void GivenInvalidTileCharacter_WhenCreatingMap_ThrowsArgumentException(
        string firstTile,
        string secondTile)
    {
        // Arrange
        string[,] rows = new string[,]
        {
            { firstTile, secondTile },
            { ".", "." }
        };

        // Act
        var exception = Assert.Throws<ArgumentException>(() => Map.FromRows(rows));

        // Assert
        exception.Message.Should().Contain("Invalid tile character: X");
    }

    [Fact]
    public void GivenWallBoundedTestMap_WhenNonSquare_ThenWallsBoundTheRequestedDimensions()
    {
        // Arrange
        var map = Map.FromRows(TestMaps.GetWallBoundedTiles(5, 3));

        // Assert
        map.Width.Should().Be(5);
        map.Height.Should().Be(3);

        map.IsWalkable(new TilePosition(0, 1)).Should().BeFalse();
        map.IsWalkable(new TilePosition(4, 1)).Should().BeFalse();
        map.IsWalkable(new TilePosition(1, 0)).Should().BeFalse();
        map.IsWalkable(new TilePosition(3, 2)).Should().BeFalse();

        map.IsWalkable(new TilePosition(1, 1)).Should().BeTrue();
        map.IsWalkable(new TilePosition(3, 1)).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(-1, 1, false)]
    [InlineData(1, -1, false)]
    public void GivenMap_WhenVerifyTileWalkable_ReturnsExpectedResult(int x, int y, bool expected)
    {
        // Arrange
        var map = Map.FromRows(TestMaps.GetWallBoundedTiles(20, 20));

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
        var map = Map.FromRows(TestMaps.GetWallBoundedTiles(20, 20));

        // Act
        var result = map.IsInBounds(new TilePosition(x, y));

        // Assert
        result.Should().Be(expected);
    }
}
