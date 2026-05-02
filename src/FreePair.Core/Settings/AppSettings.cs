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
    /// Absolute path to the FreePair USCF pairing engine binary
    /// (<c>FreePair.UscfEngine.exe</c>). Optional — when blank,
    /// <see cref="Bbp.BbpPairingEngine.ResolveEffectivePathFor"/> probes
    /// for the bundled exe next to the FreePair install. Surfaced in
    /// Settings alongside the BBP path so a TD running from a manual
    /// build can point at a custom location.
    /// </summary>
    public string? UscfEngineBinaryPath { get; set; }

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

    // ============ USCF export defaults ============
    // Persisted across sessions so the export dialog pre-fills with
    // the same TD / affiliate / venue info the TD used last time.
    // Per-event fields (event id, send-cross-table flag) stay
    // dialog-only since they vary by submission.

    public string? UscfChiefTdId { get; set; }
    public string? UscfAssistantTdId { get; set; }
    public string? UscfAffiliateId { get; set; }
    public string? UscfCity { get; set; }
    public string? UscfState { get; set; }
    public string? UscfZipCode { get; set; }
    public string? UscfCountry { get; set; } = "USA";

    /// <summary>
    /// Set to <c>true</c> after the TD dismisses the first-run
    /// "FreePair uses BBP / FIDE Dutch" disclosure dialog with the
    /// "Don't show this again" option. Defaults to <c>false</c> so
    /// fresh installs always see the disclosure on first launch
    /// (we want every TD to know up-front which pairing engine
    /// they're using and how it relates to USCF rules). The
    /// dialog also exposes a "Show this again every launch"
    /// option which simply leaves this flag at <c>false</c>.
    /// </summary>
    public bool HasAcknowledgedPairingEngineNotice { get; set; }

    // ============ Auto-update ============

    /// <summary>
    /// When <c>true</c> (default), FreePair silently polls the
    /// configured GitHub Releases feed on startup and surfaces a
    /// banner in the main window when a newer version is available.
    /// TDs uncomfortable with phone-home behaviour can flip this
    /// off; the Help → Check for updates… menu item still works
    /// on demand. The startup check is fire-and-forget — it never
    /// blocks the UI even on a slow / offline network.
    /// </summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Full GitHub repository URL hosting FreePair releases.
    /// Defaults to the canonical publisher
    /// (<c>https://github.com/freepairchess/FreePair</c>); forks
    /// can override per-install by editing settings.json or via
    /// the Settings UI. Used by Velopack's <c>GithubSource</c>.
    /// </summary>
    public string UpdateFeedRepoUrl { get; set; } = "https://github.com/freepairchess/FreePair";

    /// <summary>
    /// When <c>true</c>, the update check considers pre-release
    /// GitHub releases. Off by default — stable-channel TDs
    /// don't want to be pulled into preview builds. Beta testers
    /// flip this on per-install.
    /// </summary>
    public bool UpdateIncludePreReleases { get; set; }
}
