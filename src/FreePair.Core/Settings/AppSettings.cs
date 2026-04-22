namespace FreePair.Core.Settings;

/// <summary>
/// Persisted user settings for FreePair.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Absolute path to the BBP pairing engine binary chosen by the user.
    /// </summary>
    public string? PairingEngineBinaryPath { get; set; }

    /// <summary>
    /// Absolute path to the most recently opened SwissSys <c>.sjson</c>
    /// tournament file. Used to auto-load on application startup.
    /// </summary>
    public string? LastTournamentFilePath { get; set; }

    /// <summary>
    /// When <c>true</c> (default), scores and pairing results are rendered
    /// using pure ASCII (e.g. <c>1/2</c>, <c>1/2-1/2</c>). When <c>false</c>,
    /// Unicode glyphs are used (<c>½</c>, <c>½-½</c>).
    /// </summary>
    public bool UseAsciiOnly { get; set; } = true;
}
