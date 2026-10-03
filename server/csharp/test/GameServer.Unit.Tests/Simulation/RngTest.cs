using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class RngTest
{
    [Fact]
    public void GivenTheSameSeed_WhenDrawing_ThenTheStreamsAreIdentical()
    {
        var rng1 = new Rng(1);
        var rng2 = new Rng(1);

        for (int i = 0; i < 10; i++)
            rng1.Next().Should().Be(rng2.Next());
    }

    [Fact]
    public void GivenDifferentSeeds_WhenDrawing_ThenTheStreamsDiffer()
    {
        var rng1 = new Rng(1);
        var rng2 = new Rng(2);

        bool allEqual = true;
        for (int i = 0; i < 10; i++)
        {
            if (rng1.Next() != rng2.Next())
            {
                allEqual = false;
                break;
            }
        }

        allEqual.Should().BeFalse();
    }

    [Fact]
    public void GivenSeedZero_WhenDrawnFrom_ThenTheStreamDoesNotFreeze()
    {
        // Arrange
        var rng = new Rng(0);

        // Act
        var draws = Enumerable.Range(0, 10).Select(_ => rng.Next()).ToList();

        // Assert
        draws.Should().OnlyContain(draw => draw != 0);
        draws.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GivenSeedZeroAndSeedOne_WhenDrawnFrom_ThenTheStreamsDiffer()
    {
        // Arrange
        var zero = new Rng(0);
        var one = new Rng(1);

        // Act
        var zeroDraws = Enumerable.Range(0, 10).Select(_ => zero.Next()).ToList();
        var oneDraws = Enumerable.Range(0, 10).Select(_ => one.Next()).ToList();

        // Assert
        zeroDraws.Should().NotEqual(oneDraws);
    }

    [Fact]
    public void GivenAnySeed_WhenConstructed_ThenTheSeedIsReportedBackUnchanged()
    {
        foreach (var seed in new[] { 0, 1, -1, int.MinValue, int.MaxValue })
            new Rng(seed).Seed.Should().Be(seed);
    }

    [Fact]
    public void GivenABoundOfOne_WhenNextInt32_ThenZeroIsAlwaysReturned()
    {
        var rng = new Rng(12345);

        for (var i = 0; i < 100; i++)
            rng.NextInt32(1).Should().Be(0, "the only value in [0, 1) is 0");
    }

    [Fact]
    public void GivenABoundOfZero_WhenNextInt32_ThenItThrows()
    {
        var rng = new Rng(12345);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt32(0));
    }

    [Fact]
    public void GivenProbabilityZero_WhenChance_ThenItIsAlwaysFalse()
    {
        var rng = new Rng(12345);

        for (var i = 0; i < 100; i++)
            rng.Chance(0).Should().BeFalse();
    }

    [Fact]
    public void GivenProbabilityOne_WhenChance_ThenItIsAlwaysTrue()
    {
        var rng = new Rng(12345);

        for (var i = 0; i < 100; i++)
            rng.Chance(1).Should().BeTrue();
    }

    [Fact]
    public void GivenAnySeed_WhenNextDouble_ThenTheValueStaysWithinTheUnitInterval()
    {
        var rng = new Rng(12345);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.NextDouble();
            value.Should().BeGreaterThanOrEqualTo(0.0);
            value.Should().BeLessThan(1.0);
        }
    }
}
