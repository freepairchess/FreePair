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

    // ============ Online publishing defaults ============
    // These are app-wide DEFAULTS. Each tournament's Publish dialog
    // inherits these values on open but can override them per-event.

    /// <summary>
    /// Default NAChessHub base URL used by the Publish dialog when
    /// no tournament-level override is set. Editable in the Settings
    /// tab.
    /// </summary>
    public string NaChessHubBaseUrl { get; set; } = "https://nachesshub.com";

    /// <summary>
    /// When <c>true</c>, newly-opened tournaments start with
    /// "auto-publish pairings" enabled. The TD can still toggle it
    /// per-event from the Publish dialog.
    /// </summary>
    public bool AutoPublishPairingsDefault { get; set; }

    /// <summary>
    /// When <c>true</c>, newly-opened tournaments start with
    /// "auto-publish results" enabled.
    /// </summary>
    public bool AutoPublishResultsDefault { get; set; }

    // ============ File-system layout ============

    /// <summary>
    /// Root folder under which FreePair creates per-event subfolders
    /// (one per tournament). The <c>.sjson</c> + exported PDFs for a
    /// given event all live in the same folder so the TD can share or
    /// back up the whole event as a single directory.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> or blank, the resolver falls back to the
    /// built-in default <c>Documents/FreePairEvents</c> — see
    /// <see cref="Tournaments.TournamentFolder"/>.
    /// </remarks>
    public string? TournamentsRootFolder { get; set; }
}
