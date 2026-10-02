namespace GameServer.Unit.Tests.Helpers;

/// <summary>
/// Runs an action with <see cref="Console.Out"/> redirected into a string and returns what was
/// written. The single place a test reads back the simulation's per-tick logging.
/// </summary>
/// <remarks>
/// Callers must belong to <see cref="Collections.ConsoleOutputCollection"/>, and that is the
/// whole safety argument rather than a formality. <c>Console.Out</c> is process-global, and the
/// <see cref="StringBuilder"/> behind the <see cref="StringWriter"/> is not thread-safe, so a class
/// that reaches <c>Simulation.Tick()</c> outside the collection writes into the very writer a
/// capturing test is still reading. That does not surface as a failed assertion: it tears the
/// builder's chunk bookkeeping and throws <c>ArgumentOutOfRangeException (Parameter
/// 'chunkLength')</c> out of <see cref="StringBuilder.ToString"/>, in an unrelated test, at a
/// random point in the run.
/// <para>
/// Restoring <c>original</c> is safe under the same condition and only under it. Because no two
/// captures overlap, the writer each one saves is the process's real stdout, so restoring cannot
/// re-install a foreign writer that another thread is still appending to.
/// </para>
/// </remarks>
public static class ConsoleCapture
{
    public static string Capture(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();

        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }

    public static string Tick(GameServer.Simulation.Simulation simulation, int ticks) => Capture(() =>
    {
        for (var i = 0; i < ticks; i++) simulation.Tick();
    });
}