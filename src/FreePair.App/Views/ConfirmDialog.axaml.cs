using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the prompt text, confirm-button label, and dialog title.
    /// </summary>
    public void Configure(string title, string message, string confirmLabel = "Confirm")
    {
        Title = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmLabel;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close((bool?)true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close((bool?)false);
}
