namespace GameServer.Unit.Tests.Collections;

/// <summary>
/// xUnit runs test classes in parallel by default, and <see cref="Console.Out"/> is
/// process-global. A test that swaps it out therefore captures whatever every OTHER
/// test writes during that window, not just its own output.
///
/// <see cref="Simulation.SimulationTest"/> ticks simulations and reads back the captured
/// log to assert on command ordering, so any class that ticks a simulation in parallel
/// injects its own log lines into the buffer under test and the ordering assertions fail
/// at random. Observed failure: <c>Expected positions[0] to be less than 76, but found
/// 199</c>, where the two indices came from different tests entirely.
///
/// Membership is therefore on <em>writing</em> to the console, not on reading from it.
/// Placing a console-capturing test in its own private class is not sufficient, and was
/// the gap that let this through: the capturing class was already the only one capturing,
/// but a parallel sibling was still writing into the redirected writer.
///
/// Every class that reaches <c>Simulation.Tick()</c> belongs here. Classes that neither
/// write nor read the console should stay out so they keep running in parallel.
/// </summary>
public sealed class ConsoleOutputCollection
{
    public const string Name = nameof(ConsoleOutputCollection);
}
