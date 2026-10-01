namespace GameServer.Simulation;

public sealed class Rng
{
    /// <summary>
    /// Stand-in for a zero seed: 2^64 divided by the golden ratio, the same constant splitmix64
    /// and the PCG family use to decorrelate a seed. Only the zero case reaches it, so what it
    /// actually has to be is non-zero; after <see cref="WarmUp"/> the specific value is thoroughly
    /// mixed either way.
    /// </summary>
    private const ulong ZeroSeedFallback = 0x9E3779B97F4A7C15UL;

    public int Seed { get; }
    public ulong State { get; private set; }

    public Rng(int seed)
    {
        Seed = seed;

        State = (ulong)seed;
        if (State == 0) State = ZeroSeedFallback;

        WarmUp();
    }

    private void WarmUp()
    {
        for (int i = 0; i < 10; i++) Next();
    }

    /// <summary>
    /// Marsaglia's xorshift64, using the well-known (13, 7, 17) shift triple: a 64-bit state and
    /// three shift-xor steps, so the period is the full 2^64 - 1.
    /// </summary>
    /// <remarks>
    /// Note the directions are right-left-right, not the left-right-left the shift triple is
    /// usually written in. Both direction orders are maximal-period for this triple -- verified
    /// with Berlekamp-Massey over GF(2), linear complexity 64/64 either way -- so this is a
    /// deliberate spelling of the same generator, not a weakened one. It is called out because
    /// "the triple is (13, 7, 17)" does not on its own tell you which way the arrows point, and
    /// flipping them silently changes every stream this class has ever produced without breaking
    /// any test: same period, same distribution, different numbers.
    ///
    /// Two C# details that are bugs if you get them wrong. The shifts must be on <c>ulong</c>:
    /// <c>&gt;&gt;</c> on <c>long</c> is an arithmetic shift that copies the sign bit into the high
    /// bits, and xorshift requires the logical shift -- a <c>long</c> version runs, does not crash,
    /// and is subtly wrong. And the shifts must stay outside a <c>checked</c> context, because C#
    /// discards the bits shifted past the width and <c>checked</c> would turn that into a throw.
    /// </remarks>
    public ulong Next()
    {
        var x = State;

        x ^= x >> 13;
        x ^= x << 7;
        x ^= x >> 17;

        State = x;

        return x;
    }

    /// <summary>
    /// Returns a random double between 0.0 and 1.0.
    /// This should be used for probability rolls.
    /// </summary>
    public double NextDouble()
    {
        var raw = Next();
        var top53 = raw >> 11;
        return top53 * (1.0 / (1UL << 53));
    }

    /// <summary>
    /// Returns a random integer between 0 (inclusive) and the specified bound (exclusive).
    /// This should be used for picking among a set of discrete options.
    /// </summary>
    /// <param name="bound">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>A random integer between 0 (inclusive) and the specified bound (exclusive).</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bound"/> is zero or negative. There is no integer in [0, 0) to return, so
    /// this is a caller bug rather than an empty result -- and left unguarded it would surface as a
    /// <see cref="DivideByZeroException"/> from the modulus below, which says nothing about the
    /// actual mistake. <c>Ai.Think</c> returns early when its candidate list is empty and so never
    /// reaches this, but the contract is cheap to state on the method that owns it.
    /// </exception>
    public int NextInt32(int bound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bound);

        // Rejection sampling, not a plain modulo. 2^64 is not a multiple of `bound`, so the low
        // residue values would otherwise be very slightly more likely than the high ones -- for
        // bound == 4 the skew is on the order of 1 in 10^19, which is to say irrelevant for picking
        // a direction to wander in. Truncating to the largest whole multiple of `bound` that fits
        // in 64 bits and discarding the rest costs four lines and makes the argument "obviously
        // right" rather than "negligibly wrong". Expected extra iterations: 1 in 4 billion.
        var limit = ulong.MaxValue / (ulong)bound * (ulong)bound;
        while (true)
        {
            var raw = Next();
            if (raw < limit)
                return (int)(raw % (ulong)bound);
        }
    }

    /// <summary>
    /// Returns true with the specified probability, and false otherwise.
    /// </summary>
    /// <param name="probability">The probability of returning true, between 0.0 and 1.0.</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    public bool Chance(double probability) => NextDouble() < probability;
}
