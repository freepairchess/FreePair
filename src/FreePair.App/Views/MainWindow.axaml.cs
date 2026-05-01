using System;
using Avalonia.Controls;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// Tournament path passed on the command line, set by
    /// <c>App.OnFrameworkInitializationCompleted</c> before the
    /// window is shown. When non-null, the startup sequence
    /// loads it instead of the persisted "last opened" file.
    /// </summary>
    public string? InitialTournamentPath { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        await vm.Settings.InitializeAsync();

        // First-run disclosure: explain BBP / FIDE Dutch up-front so
        // every TD knows from day one which pairing engine they're
        // using and how it relates to USCF rules. Suppressed
        // permanently once the TD checks "Don't show this again".
        if (!vm.Settings.HasAcknowledgedPairingEngineNotice)
        {
            var dontShowAgain = await new PairingEngineNoticeDialog().ShowDialog<bool>(this);
            if (dontShowAgain)
            {
                await vm.Settings.SetPairingEngineNoticeAcknowledgedAsync(true);
            }
        }

        // CLI arg wins over the persisted last-tournament path. We
        // pass skipAutoLoadLast=true so InitializeAsync doesn't race
        // ahead and try to acquire the previous instance's lock
        // (which would surface as a misleading "already open"
        // banner on the new window).
        if (!string.IsNullOrWhiteSpace(InitialTournamentPath))
        {
            await vm.Tournament.InitializeAsync(skipAutoLoadLast: true);
            await vm.Tournament.LoadFromStartupArgsAsync(InitialTournamentPath);
        }
        else
        {
            await vm.Tournament.InitializeAsync();
        }
    }
}