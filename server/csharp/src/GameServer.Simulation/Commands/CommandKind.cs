namespace GameServer.Simulation;

/// <summary>
/// Which resolution phase a <see cref="Commands.Command"/> belongs to. Used as the primary sort
/// key in <c>Simulation.Tick</c>, so the declaration order below is load-bearing: <c>Move</c> is
/// written first because movement has to resolve before combat within a tick, and reordering
/// these values would silently change tick semantics without failing any test.
/// </summary>
/// <remarks>
/// This is the same fragility <c>Ai.Think</c> calls out for <c>DirectionExtensions.All</c> --
/// enum declaration order is an implementation detail, and letting it decide an outcome means
/// the outcome depends on how the enum happens to be written. It is accepted here rather than
/// avoided with an explicit rank lookup, because the ordering is genuinely part of the rule
/// ("movement is resolved before combat") and one declared sequence states that more directly
/// than a parallel table would. The cost is that the coupling is invisible at the reorder site,
/// which is what the note above is for.
/// </remarks>
public enum CommandKind
{
    Move,
    Attack
}
