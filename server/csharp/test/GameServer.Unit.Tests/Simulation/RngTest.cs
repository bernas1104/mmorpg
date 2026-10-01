using GameServer.Simulation;

namespace GameServer.Unit.Tests.Simulation;

public sealed class RngTest
{
    [Fact]
    public void Rng_ProducesDeterministicResults()
    {
        var rng1 = new Rng(1);
        var rng2 = new Rng(1);

        for (int i = 0; i < 10; i++)
            rng1.Next().Should().Be(rng2.Next());
    }

    [Fact]
    public void Rng_ProducesDifferentResultsForDifferentSeeds()
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
        // Zero is xorshift's absorbing state, so a generator left on it emits zero forever: a
        // dead generator that still looks perfectly reproducible.
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
        // Pinning the "only zero is remapped" half of the mapping: had seed 0 been folded onto
        // some seed a human is likely to type, those two runs would be indistinguishable.
        zeroDraws.Should().NotEqual(oneDraws);
    }

    [Fact]
    public void GivenAnySeed_WhenConstructed_ThenTheSeedIsReportedBackUnchanged()
    {
        // The logged seed has to be re-typeable to replay the run, so the property reports what
        // the caller asked for, not the internal state the generator was mapped onto.
        foreach (var seed in new[] { 0, 1, -1, int.MinValue, int.MaxValue })
            new Rng(seed).Seed.Should().Be(seed);
    }
}
