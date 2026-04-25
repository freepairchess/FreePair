using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Registries;

namespace FreePair.App.ViewModels;

/// <summary>
/// View-model for the "Open from online registry" dialog (by event
/// ID + passcode). The registry combobox is populated from
/// <see cref="RegistryCatalog"/> so new providers appear
/// automatically without UI churn.
/// </summary>
public partial class OpenFromRegistryViewModel : ObservableObject
{
    public IReadOnlyList<IExternalRegistry> Registries { get; }

    [ObservableProperty] private IExternalRegistry _selectedRegistry;
    [ObservableProperty] private string _eventId = string.Empty;
    [ObservableProperty] private string _passcode = string.Empty;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// When set (browse-flow handoff), the dialog shows this event's
    /// name + dates + location + organizer + status above the form
    /// so the TD knows exactly which event they're entering the
    /// passcode for. Null in the by-id flow because we have nothing
    /// to show — just an opaque GUID.
    /// </summary>
    public RegistryEvent? PrefilledEvent { get; init; }

    /// <summary>
    /// Convenience for XAML: <c>true</c> when
    /// <see cref="PrefilledEvent"/> is non-null. Drives the
    /// IsVisible toggle on the event-context block.
    /// </summary>
    public bool HasPrefilledEvent => PrefilledEvent is not null;

    public OpenFromRegistryViewModel(IReadOnlyList<IExternalRegistry> registries)
    {
        if (registries is null || registries.Count == 0)
        {
            throw new System.ArgumentException(
                "At least one registry must be provided.", nameof(registries));
        }
        Registries = registries;
        _selectedRegistry = registries[0];
    }

    /// <summary>
    /// Validates all required fields. Sets
    /// <see cref="ErrorMessage"/> on failure.
    /// </summary>
    public bool TryValidate()
    {
        if (SelectedRegistry is null)
        {
            ErrorMessage = "Pick a registry.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(EventId))
        {
            ErrorMessage = "Event ID is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Passcode))
        {
            ErrorMessage = "Passcode is required.";
            return false;
        }
        ErrorMessage = null;
        return true;
    }
}
