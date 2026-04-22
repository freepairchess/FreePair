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