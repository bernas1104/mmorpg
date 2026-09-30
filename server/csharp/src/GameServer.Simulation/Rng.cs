namespace GameServer.Simulation;

public sealed class Rng
{
    public int Seed { get; }
    public long State { get; }

    public Rng(int seed)
    {
        // PSEUDO
        // Initialize the RNG state here
        Seed = seed;
    }

    /// <summary>
    /// Returns a random double between 0.0 and 1.0.
    /// This should be used for probability rolls.
    /// </summary>
    public double NextDouble()
    {
        // PSEUDO
        // raw = next()
        // take the TOP 53 bits of raw
        // return those 53 bits scaled by 2^(-53)
    }

    /// <summary>
    /// Returns a random integer between 0 (inclusive) and the specified bound (exclusive).
    /// This should be used for picking among a set of discrete options.
    /// </summary>
    /// <param name="bound">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>A random integer between 0 (inclusive) and the specified bound (exclusive).</returns>
    public int NextInt32(int bound)
    {
        // PSEUDO
        // limit = LARGEST multiple of bound that fits in 64 bits
        // loop:
        //     raw = next()
        //     if raw < limit: return raw % bound
    }

    /// <summary>
    /// Returns true with the specified probability, and false otherwise.
    /// </summary>
    /// <param name="probability">The probability of returning true, between 0.0 and 1.0.</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    public bool Chance(double probability)
    {
        // PSEUDO
        // return NextDouble() < probability
    }
}
