namespace FreePair.Core.Uscf.Trf;

/// <summary>
/// One per-round cell from a TRF <c>001</c> player line. The TRF cell is
/// 10 chars wide: <c> _OOOO_C_R_</c> where <c>OOOO</c> is the opponent
/// pair number (right-justified), <c>C</c> is the colour (<c>w</c>/<c>b</c>/<c>-</c>),
/// and <c>R</c> is the result code (<c>1</c>/<c>0</c>/<c>=</c>/<c>U</c>/<c>H</c>/<c>Z</c>/<c>-</c>).
/// </summary>
/// <param name="Opponent">
/// Pair number of the opponent. <c>0</c> for byes / unpaired / pre-flagged
/// half-point byes.
/// </param>
/// <param name="Color">
/// <c>'w'</c>, <c>'b'</c>, or <c>'-'</c> when no colour applies (bye /
/// unpaired).
/// </param>
/// <param name="Result">
/// Result code as written. Common values: <c>'1'</c> win, <c>'0'</c> loss,
/// <c>'='</c> draw, <c>'U'</c> full-point bye, <c>'H'</c> half-point bye,
/// <c>'Z'</c> zero-point bye, <c>'-'</c> unpaired / no result yet.
/// </param>
public readonly record struct TrfRoundCell(int Opponent, char Color, char Result)
{
    /// <summary>An empty / unplayed slot: opponent 0, colour '-', result '-'.</summary>
    public static TrfRoundCell Empty { get; } = new(0, '-', '-');

    /// <summary>True when the player was not paired in this round (no opponent and no bye).</summary>
    public bool IsUnpaired => Opponent == 0 && Result == '-';

    /// <summary>True when the cell represents a bye of any flavour.</summary>
    public bool IsBye => Result is 'U' or 'H' or 'Z';

    /// <summary>
    /// Score the player received for this round, in standard 1 / 0.5 / 0
    /// terms (independent of forfeit / regular distinction).
    /// </summary>
    public decimal Score => Result switch
    {
        '1' => 1m,
        'U' => 1m,   // full-point bye
        '=' => 0.5m,
        'H' => 0.5m, // half-point bye
        _   => 0m,
    };
}
