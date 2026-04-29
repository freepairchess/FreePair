using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class UscfExportDialog : Window
{
    public UscfExportDialog()
    {
        InitializeComponent();
    }

    public UscfExportDialog(UscfExportViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UscfExportViewModel vm)
        {
            Close((UscfExportViewModel?)null);
            return;
        }
        if (!vm.TryValidate()) return;
        Close(vm);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((UscfExportViewModel?)null);

    /// <summary>
    /// Look-up click on one of the 🔗 buttons next to an ID field.
    /// Routes to the USCF member-services URL using the field's
    /// current VM value (so the link reflects the latest TD edit,
    /// not the dialog's pre-fill). Best-effort — silently ignores
    /// when there's no default browser registered.
    /// </summary>
    private void OnLookupIdClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UscfExportViewModel vm) return;
        if (sender is not Avalonia.Controls.Button b) return;

        var (id, kind) = (b.Tag as string) switch
        {
            "affiliate" => (vm.AffiliateId,   "affiliate"),
            "chief"     => (vm.ChiefTdId,     "player"),
            "assistant" => (vm.AssistantTdId, "player"),
            _           => (string.Empty,     string.Empty),
        };
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrEmpty(kind)) return;

        var url = $"https://ratings.uschess.org/{kind}/{System.Uri.EscapeDataString(id.Trim())}";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch
        {
            // No default browser registered / sandboxed shell — fine.
        }
    }
}
