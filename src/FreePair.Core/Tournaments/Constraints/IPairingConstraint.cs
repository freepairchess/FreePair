namespace FreePair.Core.Tournaments.Constraints;

/// <summary>
/// A predicate describing when two players should not be paired against
/// each other in a given round. The post-BBP <see cref="PairingSwapper"/>
/// evaluates the active constraint set against every proposed pairing
/// and attempts a colour-stable, same-score-group 1-swap to resolve any
/// violation.
/// </summary>
/// <remarks>
/// Constraints are <em>soft</em>: the swapper honours them only when a
/// valid replacement pairing exists in the same score group. Otherwise
/// the violating pairing is left in place and surfaced via
/// <see cref="PairingSwapResult.UnresolvedConflicts"/> so the TD can
/// decide whether to accept or manually re-pair.
/// </remarks>
public interface IPairingConstraint
{
    /// <summary>
    /// Returns <c>true</c> when pairing <paramref name="a"/> against
    /// <paramref name="b"/> would violate this constraint. Order-
    /// insensitive: implementations must return the same answer for
    /// <c>(a, b)</c> and <c>(b, a)</c>.
    /// </summary>
    bool Violates(Player a, Player b);

    /// <summary>
    /// A short, TD-readable reason string used when the constraint
    /// cannot be resolved (e.g. <c>"same team 'Daas'"</c>).
    /// </summary>
    string Describe(Player a, Player b);
}
