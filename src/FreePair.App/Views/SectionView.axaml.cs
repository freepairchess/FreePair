using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FreePair.App.ViewModels;
using FreePair.Core.Formatting;
using FreePair.Core.Reports;
using FreePair.Core.Tournaments;

namespace FreePair.App.Views;

public partial class SectionView : UserControl
{
    public SectionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handler for the small "⧉" copy buttons embedded in Players-tab
    /// cells (USCF, Name, Rating, Team, Email, Phone). Reads the value
    /// to copy from the sender's <see cref="Control.Tag"/>, coerces it
    /// to a string (so both string bindings and the numeric Rating
    /// work), and writes it to the application clipboard via the
    /// hosting <see cref="TopLevel"/>. Uses Avalonia 12's
    /// <see cref="DataTransfer"/> + <see cref="IClipboard.SetDataAsync"/>
    /// API.
    /// </summary>
    private async void OnCopyFieldClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var text = btn.Tag switch
        {
            string s => s,
            null => null,
            var other => other.ToString(),
        };
        if (string.IsNullOrEmpty(text)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;
        try
        {
            var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.CreateText(text));
            await clipboard.SetDataAsync(transfer);
        }
        catch { /* best effort — clipboard access can fail on some hosts */ }
    }

    /// <summary>
    /// Clears the Players-tab filter text box via its bound VM property.
    /// </summary>
    private void OnClearPlayerFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm)
        {
            vm.PlayerFilter = string.Empty;
        }
    }

    private void OnClearPairingFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm) vm.PairingFilter = string.Empty;
    }

    private void OnClearStandingsFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm) vm.StandingsFilter = string.Empty;
    }

    private void OnClearWallChartFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm) vm.WallChartFilter = string.Empty;
    }

    private void OnClearByesFilter(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SectionViewModel vm) vm.ByesFilter = string.Empty;
    }

    // =========================================================
    //  Print as PDF
    // =========================================================

    private SectionViewModel? Vm => DataContext as SectionViewModel;

    /// <summary>
    /// Opens a Save-As dialog scoped to PDFs and returns the picked
    /// path, or null when the user cancels / no hosting window exists.
    /// Filename suggestion is sanitised to strip filesystem-invalid
    /// characters.
    /// </summary>
    private async Task<string?> PickPdfPathAsync(string suggestedName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var safe = string.Concat(suggestedName.Split(Path.GetInvalidFileNameChars()));
        if (!safe.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) safe += ".pdf";

        var options = new FilePickerSaveOptions
        {
            Title = "Save PDF report",
            SuggestedFileName = safe,
            DefaultExtension = "pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF document") { Patterns = new[] { "*.pdf" } },
                FilePickerFileTypes.All,
            },
        };
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    /// <summary>
    /// Runs a PDF-writing delegate inside a try/catch that routes any
    /// error into the parent tournament VM's ErrorMessage so the TD
    /// sees a banner instead of an unhandled exception. On success the
    /// file is written to <paramref name="path"/> and the status area
    /// gets a short confirmation.
    /// </summary>
    private void WritePdf(string path, Action<Stream> write)
    {
        try
        {
            using var fs = File.Create(path);
            write(fs);
            if (Vm?.ParentTournamentVm is { } parent)
            {
                parent.SaveStatus = $"PDF saved: {Path.GetFileName(path)}";
            }
        }
        catch (Exception ex)
        {
            if (Vm?.ParentTournamentVm is { } parent)
            {
                parent.ErrorMessage = $"Failed to write PDF: {ex.Message}";
            }
        }
    }

    private IScoreFormatter Formatter =>
        Vm?.Formatter ?? new FreePair.Core.Formatting.ScoreFormatter();

    private Tournament? TournamentSnapshot =>
        Vm?.ParentTournamentVm?.Tournament;

    private async void OnPrintPlayersPdf(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || TournamentSnapshot is null) return;
        var path = await PickPdfPathAsync($"{Vm.Name}-players");
        if (path is null) return;
        WritePdf(path, s => PdfReportBuilder.WritePlayersReport(s, TournamentSnapshot, Vm.Section));
    }

    private async void OnPrintPairingsPdf(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || TournamentSnapshot is null || Vm.SelectedRound is null) return;
        var round = Vm.SelectedRound.Number;
        var path = await PickPdfPathAsync($"{Vm.Name}-round-{round}-pairings");
        if (path is null) return;
        WritePdf(path, s => PdfReportBuilder.WritePairingsReport(s, TournamentSnapshot, Vm.Section, round));
    }

    private async void OnPrintStandingsPdf(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || TournamentSnapshot is null) return;
        var path = await PickPdfPathAsync($"{Vm.Name}-standings");
        if (path is null) return;
        WritePdf(path, s => PdfReportBuilder.WriteStandingsReport(s, TournamentSnapshot, Vm.Section, Formatter));
    }

    private async void OnPrintWallChartPdf(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || TournamentSnapshot is null) return;
        var path = await PickPdfPathAsync($"{Vm.Name}-wallchart");
        if (path is null) return;
        WritePdf(path, s => PdfReportBuilder.WriteWallChartReport(s, TournamentSnapshot, Vm.Section, Formatter));
    }

    private async void OnPrintPrizesPdf(object? sender, RoutedEventArgs e)
    {
        if (Vm is null || TournamentSnapshot is null) return;
        var path = await PickPdfPathAsync($"{Vm.Name}-prizes");
        if (path is null) return;
        WritePdf(path, s => PdfReportBuilder.WritePrizesReport(s, TournamentSnapshot, Vm.Section));
    }
}
