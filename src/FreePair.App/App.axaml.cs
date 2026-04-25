using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using FreePair.App.ViewModels;
using FreePair.App.Views;
using FreePair.Core.Bbp;
using FreePair.Core.Formatting;
using FreePair.Core.Settings;
using FreePair.Core.Tournaments;
using Microsoft.Extensions.DependencyInjection;

namespace FreePair.App;

public partial class App : Application
{
    /// <summary>
    /// Root service provider built during <see cref="OnFrameworkInitializationCompleted"/>.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            // Auto-load on startup when a tournament path is passed
            // on the command line — this is how the multi-instance
            // flow re-routes "Open" / "Open from online" clicks made
            // in an instance that already has a tournament open.
            // Only the first arg is consumed; everything else is
            // ignored. Fired after the main window is wired so
            // ErrorMessage banner / WindowTitle binding pick up.
            if (desktop.Args is { Length: > 0 } args &&
                !string.IsNullOrWhiteSpace(args[0]))
            {
                var initialPath = args[0];
                desktop.MainWindow.Opened += async (_, _) =>
                {
                    var vm = Services.GetRequiredService<TournamentViewModel>();
                    await vm.LoadFromStartupArgsAsync(initialPath);
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ITournamentLoader, TournamentLoader>();
        services.AddSingleton<ITournamentWriter, SwissSysTournamentWriter>();
        services.AddSingleton<IScoreFormatter, ScoreFormatter>();
        services.AddSingleton<IBbpPairingEngine, BbpPairingEngine>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<TournamentViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }
}