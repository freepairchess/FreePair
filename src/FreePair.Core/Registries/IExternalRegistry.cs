using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreePair.Core.Registries;

/// <summary>
/// Abstraction over an external chess event registry / tournament
/// management site that FreePair can open tournaments from.
/// NA Chess Hub is the first implementation; we expect to add
/// Chess-Results, ChessRoster, etc. behind the same interface
/// without touching the UI layer.
/// </summary>
/// <remarks>
/// <para>Two capability contracts:</para>
/// <list type="bullet">
/// <item><b>Download by event ID + passcode</b> — required for every
///       provider (the "power-user" flow where the TD already knows
///       the credentials).</item>
/// <item><b>List events</b> — optional (capability flag
///       <see cref="SupportsListEvents"/>). Some providers only
///       expose single-event downloads; for those the browse UI
///       is hidden.</item>
/// </list>
/// </remarks>
public interface IExternalRegistry
{
    /// <summary>Stable key for settings / logging (e.g. <c>"nachesshub"</c>).</summary>
    string Key { get; }

    /// <summary>Human-readable name shown in the UI combobox.</summary>
    string DisplayName { get; }

    /// <summary>
    /// True when <see cref="ListEventsAsync"/> is supported — the
    /// UI hides the "Browse events" button for providers that
    /// return false.
    /// </summary>
    bool SupportsListEvents { get; }

    /// <summary>
    /// Downloads the SwissSys <c>.sjson</c> payload for
    /// <paramref name="eventId"/>, authenticated by
    /// <paramref name="passcode"/>. Returns the raw bytes — the
    /// caller is responsible for writing them to disk under the
    /// appropriate tournament folder.
    /// </summary>
    Task<byte[]> DownloadSjsonAsync(
        string eventId,
        string passcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists events visible via the registry's public "events"
    /// endpoint (no passcode required). Throws
    /// <see cref="NotSupportedException"/> when
    /// <see cref="SupportsListEvents"/> is false.
    /// </summary>
    Task<IReadOnlyList<RegistryEvent>> ListEventsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the public web URL the TD can hit in a browser to
    /// see an event's details page (separate from the API URL —
    /// e.g. NA Chess Hub uses <c>www.nachesshub.com</c> for the
    /// website and <c>nachesshub.com</c> for the API root).
    /// Returns <c>null</c> when the registry doesn't have a
    /// browsable event page.
    /// </summary>
    string? GetEventWebUrl(string eventId);
}

/// <summary>
/// One event surfaced by a registry's <c>ListEvents</c> endpoint.
/// Only <see cref="Id"/> and <see cref="Name"/> are required; the
/// rest are display-only hints. Providers are expected to map
/// their native field names onto this DTO.
/// </summary>
/// <remarks>
/// <see cref="City"/>, <see cref="State"/>, and <see cref="ZipCode"/>
/// are kept as separate fields so the browse-events grid can sort
/// each column independently. <see cref="Location"/> is the
/// composite display string ("City, ST 01752") providers can fill
/// when their native API doesn't split the address (or when a
/// downstream consumer just wants a single line — see the
/// passcode dialog's event-context block).
/// </remarks>
public sealed record RegistryEvent(
    string Id,
    string Name,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? Location = null,
    string? City = null,
    string? State = null,
    string? ZipCode = null,
    string? Organizer = null,
    string? Status = null);
