namespace GameServer.Simulation;

public static class SimulationConstants
{
    public const int TickDurationMs = 50;       // 50ms per tick

    // Probability that an npc that is past its movement cooldown actually commits to a step.
    //
    // The cooldown is the floor here, not this constant: the ai's gate refuses to roll at all
    // until Movement.MoveCooldownTicks have elapsed, so a miss costs a tick but never a rejection,
    // and the noisiest failure mode an ungated roll would produce cannot occur. Measured over 500
    // ticks at this value: mean gap between steps 13.5 ticks, minimum exactly the 10-tick
    // cooldown. Read that as roughly one step every 700ms -- a shamble rather than a patrol.
    //
    // Raising it to 0.1 would stretch the mean gap to ~19 ticks (~950ms) and is the value to
    // reach for if the wander reads as twitchy. Lowering it much further starts to look like
    // jitter, since it buys motion the cooldown was never going to let through.
    public const double WanderChance = 0.2;
}
