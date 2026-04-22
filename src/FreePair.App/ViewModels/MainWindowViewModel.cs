using System;

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
    }

    public string Greeting { get; } = "FreePair";

    public TournamentViewModel Tournament { get; }

    public SettingsViewModel Settings { get; }
}
