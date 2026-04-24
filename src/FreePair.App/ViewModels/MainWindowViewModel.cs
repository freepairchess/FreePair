using System;
using System.ComponentModel;

namespace FreePair.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
        : this(new TournamentViewModel(), new SettingsViewModel())
    {
    }

    public MainWindowViewModel(TournamentViewModel tournament, SettingsViewModel settings)
    {
        Tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // When the user flips the ASCII / Unicode toggle, rebuild the
        // Section view models so the change is visible immediately.
        Settings.FormatPreferenceChanged = Tournament.RebuildSections;

        // Re-emit WindowTitle whenever the nested tournament loads,
        // closes, or has its overview Title edited via the event
        // config dialog.
        Tournament.PropertyChanged += OnTournamentPropertyChanged;
    }

    public string Greeting { get; } = "FreePair";

    public TournamentViewModel Tournament { get; }

    public SettingsViewModel Settings { get; }

    /// <summary>
    /// Text shown in the OS window title bar. "FreePair" when no
    /// tournament is loaded; "FreePair — {Title}" when one is open.
    /// Falls back to the file name (sans .sjson) for untitled
    /// tournaments so TDs running multiple instances can still tell
    /// them apart.
    /// </summary>
    public string WindowTitle
    {
        get
        {
            var t = Tournament.Tournament;
            if (t is null) return "FreePair";

            var label = !string.IsNullOrWhiteSpace(t.Title)
                ? t.Title!
                : !string.IsNullOrWhiteSpace(Tournament.CurrentFilePath)
                    ? System.IO.Path.GetFileNameWithoutExtension(Tournament.CurrentFilePath)!
                    : "(untitled)";
            return $"FreePair — {label}";
        }
    }

    private void OnTournamentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Tournament loaded / closed → re-bind title. The Tournament
        // record itself is immutable so Overview title edits always
        // go through a 'Tournament = ... with { Title = ... }'
        // assignment, which raises PropertyChanged on the Tournament
        // property — no need to listen per-field.
        if (e.PropertyName is nameof(TournamentViewModel.Tournament)
                           or nameof(TournamentViewModel.CurrentFilePath))
        {
            OnPropertyChanged(nameof(WindowTitle));
        }
    }
}
