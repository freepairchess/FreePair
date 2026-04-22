using System;
using Avalonia.Controls;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.Settings.InitializeAsync();
            await vm.Tournament.InitializeAsync();
        }
    }
}