using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FreePair.Core.SwissSys.Raw;

namespace FreePair.Core.SwissSys;

/// <summary>
/// Loads a SwissSys <c>.sjson</c> file from disk (or any stream) into a
/// <see cref="RawSwissSysDocument"/> without interpreting the content. Higher
/// layers are expected to map the raw document into the FreePair domain
/// model.
/// </summary>
public class SwissSysImporter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads and parses a SwissSys <c>.sjson</c> file from the given path.
    /// </summary>
    public async Task<RawSwissSysDocument> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path must be provided.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"SwissSys file not found: {filePath}", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        return await ImportAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses a SwissSys <c>.sjson</c> payload from an open stream.
    /// </summary>
    public async Task<RawSwissSysDocument> ImportAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            var document = await JsonSerializer.DeserializeAsync<RawSwissSysDocument>(
                stream, s_jsonOptions, cancellationToken).ConfigureAwait(false);

            if (document is null)
            {
                throw new InvalidDataException("SwissSys file was empty or null.");
            }

            return document;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"SwissSys file is not valid JSON: {ex.Message}", ex);
        }
    }
}
