using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Registries;

namespace FreePair.App.ViewModels;

/// <summary>
/// View-model for the "Browse online events" dialog. Lets the TD
/// pick a registry, lists its public events, and — on OK — collects
/// the passcode so the caller can kick off a download. Populating
/// the list is the host's job (it runs the async call and pushes
/// the results via <see cref="SetEvents"/>); the VM stays
/// synchronous so it's easy to unit-test.
/// </summary>
public partial class BrowseRegistryEventsViewModel : ObservableObject
{
    public IReadOnlyList<IExternalRegistry> Registries { get; }

    [ObservableProperty]
    private IExternalRegistry _selectedRegistry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEvents))]
    private bool _isLoading;

    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _filter = string.Empty;

    /// <summary>
    /// The event the TD picked by clicking its row-level Open
    /// button. Set by the dialog code-behind right before it closes
    /// so the caller can read it off the result VM.
    /// </summary>
    public RegistryEvent? ChosenEvent { get; set; }

    /// <summary>All events returned by the most recent list call.</summary>
    public ObservableCollection<RegistryEvent> Events { get; } = new();

    /// <summary>
    /// Events after applying <see cref="Filter"/> (substring match
    /// on Name / Id / Location / Organizer, case-insensitive).
    /// Rebuilt on every <see cref="SetEvents"/> call and on every
    /// filter-text change.
    /// </summary>
    public ObservableCollection<RegistryEvent> FilteredEvents { get; } = new();

    public bool HasEvents => Events.Count > 0 && !IsLoading;

    public BrowseRegistryEventsViewModel(IReadOnlyList<IExternalRegistry> registries)
    {
        if (registries is null || registries.Count == 0)
        {
            throw new System.ArgumentException(
                "At least one registry must be provided.", nameof(registries));
        }
        Registries = registries.Where(r => r.SupportsListEvents).ToArray();
        if (Registries.Count == 0)
        {
            throw new System.ArgumentException(
                "No registry supports listing events.", nameof(registries));
        }
        _selectedRegistry = Registries[0];
    }

    /// <summary>
    /// Replaces the event list. Called by the host after a
    /// successful <see cref="IExternalRegistry.ListEventsAsync"/>.
    /// </summary>
    public void SetEvents(IEnumerable<RegistryEvent> events)
    {
        Events.Clear();
        foreach (var e in events) Events.Add(e);
        ApplyFilter();
        OnPropertyChanged(nameof(HasEvents));
    }

    partial void OnFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredEvents.Clear();
        if (string.IsNullOrWhiteSpace(Filter))
        {
            foreach (var e in Events) FilteredEvents.Add(e);
            return;
        }
        var q = Filter.Trim();
        foreach (var e in Events)
        {
            if (Match(e.Name, q) || Match(e.Id, q) || Match(e.Location, q) || Match(e.Organizer, q))
            {
                FilteredEvents.Add(e);
            }
        }
    }

    private static bool Match(string? field, string q) =>
        !string.IsNullOrEmpty(field) &&
        field.Contains(q, System.StringComparison.OrdinalIgnoreCase);
}
