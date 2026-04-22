using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// Combo-box item for pairing result entry — pairs an enum value with
/// its display text (formatter-aware, respects the ASCII / Unicode
/// toggle).
/// </summary>
public sealed record PairingResultOption(PairingResult Value, string Text)
{
    public override string ToString() => Text;
}
