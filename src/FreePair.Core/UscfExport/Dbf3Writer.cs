using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FreePair.Core.UscfExport;

/// <summary>
/// Minimal writer for dBase III (<c>.DBF</c>, version byte
/// <c>0x03</c>, no memo) — the exact format USCF accepts for
/// tournament submissions and that SwissSys 11 produces. Supports
/// only Character (<c>C</c>) fields, which is all USCF needs:
/// every "numeric" column in the THEXPORT / TSEXPORT / TDEXPORT
/// schemas is actually stored as a left-justified, space-padded
/// ASCII string in a Character field.
/// </summary>
/// <remarks>
/// <para>File layout written:</para>
/// <list type="bullet">
///   <item>32-byte header (version, last-update YYMMDD, record count, header size, record size).</item>
///   <item><c>N</c> × 32-byte field descriptors.</item>
///   <item>1-byte header terminator <c>0x0D</c>.</item>
///   <item>Records, each prefixed with <c>0x20</c> (active) and field bytes concatenated.</item>
///   <item>1-byte file terminator <c>0x1A</c>.</item>
/// </list>
/// </remarks>
public sealed class Dbf3Writer
{
    /// <summary>One Character field in the schema.</summary>
    public sealed record Field(string Name, int Length)
    {
        // dBase III field names are at most 10 ASCII chars, NUL-padded.
        public byte[] EncodeName()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            if (Name.Length > 10)
            {
                throw new ArgumentException(
                    $"DBF field name '{Name}' exceeds the 10-char limit.", nameof(Name));
            }
            var bytes = new byte[11];
            Encoding.ASCII.GetBytes(Name, bytes);
            return bytes;
        }
    }

    private readonly IReadOnlyList<Field> _fields;

    public Dbf3Writer(IReadOnlyList<Field> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Count == 0)
        {
            throw new ArgumentException("DBF requires at least one field.", nameof(fields));
        }
        _fields = fields;
    }

    /// <summary>
    /// Sum of field lengths plus 1 byte for the per-record
    /// deletion flag — i.e. the total bytes each row occupies.
    /// </summary>
    public int RecordLength
    {
        get
        {
            var n = 1; // deletion flag
            foreach (var f in _fields) n += f.Length;
            return n;
        }
    }

    /// <summary>
    /// Bytes from start-of-file to the first record. Equals
    /// 32 (file header) + 32 × fields + 1 (terminator).
    /// </summary>
    public int HeaderLength => 32 + 32 * _fields.Count + 1;

    /// <summary>
    /// Writes the full file. Each row is a string-keyed lookup of
    /// field name → field value; missing keys are treated as
    /// empty (all-spaces). Values longer than the field length are
    /// truncated with no warning (USCF DBFs are fixed-width and
    /// SwissSys silently truncates).
    /// </summary>
    public void Write(Stream output, IReadOnlyList<IReadOnlyDictionary<string, string>> rows, DateOnly? lastUpdate = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(rows);

        var update = lastUpdate ?? DateOnly.FromDateTime(DateTime.Today);

        // === File header (32 bytes) ===
        Span<byte> header = stackalloc byte[32];
        header[0] = 0x03;                              // dBase III, no memo
        header[1] = (byte)(update.Year % 100);         // YY (years since 1900 by convention; SwissSys also stores % 100)
        header[2] = (byte)update.Month;
        header[3] = (byte)update.Day;
        BinaryPrimitives_WriteUInt32LE(header.Slice(4, 4), (uint)rows.Count);
        BinaryPrimitives_WriteUInt16LE(header.Slice(8, 2), (ushort)HeaderLength);
        BinaryPrimitives_WriteUInt16LE(header.Slice(10, 2), (ushort)RecordLength);
        // Bytes 12..31 stay zero (reserved / unused for dBase III).
        output.Write(header);

        // === Field descriptors (32 bytes each) ===
        Span<byte> fd = stackalloc byte[32];
        foreach (var f in _fields)
        {
            fd.Clear();
            f.EncodeName().AsSpan(0, 11).CopyTo(fd[..11]);
            fd[11] = (byte)'C';            // type: Character
            // Field data address (12..15) is not used by dBase III readers in practice.
            fd[16] = (byte)f.Length;       // length
            fd[17] = 0;                    // decimal count
            // 18..31 reserved
            output.Write(fd);
        }

        // === Header terminator ===
        output.WriteByte(0x0D);

        // === Records ===
        foreach (var row in rows)
        {
            output.WriteByte(0x20); // active record (0x2A would mark deleted)
            foreach (var f in _fields)
            {
                var value = row.TryGetValue(f.Name, out var v) ? v : string.Empty;
                WritePaddedAscii(output, value, f.Length);
            }
        }

        // === EOF marker ===
        output.WriteByte(0x1A);
    }

    /// <summary>
    /// Writes <paramref name="value"/> as ASCII into a fixed-
    /// length, left-justified, space-padded slot of
    /// <paramref name="length"/> bytes. Non-ASCII chars become
    /// '?', mirroring SwissSys's behaviour on names with diacritics.
    /// </summary>
    private static void WritePaddedAscii(Stream output, string value, int length)
    {
        Span<byte> buf = length <= 256 ? stackalloc byte[length] : new byte[length];
        for (var i = 0; i < length; i++) buf[i] = 0x20;

        if (!string.IsNullOrEmpty(value))
        {
            // Encoding.ASCII silently maps non-ASCII to '?' which
            // is the correct (lossy) behaviour for USCF DBFs that
            // only accept the ASCII subset.
            var truncated = value.Length > length ? value[..length] : value;
            var written = Encoding.ASCII.GetBytes(truncated, buf);
            // Tail (length - written) stays as the pre-filled spaces.
            _ = written;
        }
        output.Write(buf);
    }

    private static void BinaryPrimitives_WriteUInt32LE(Span<byte> dest, uint value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(dest, value);

    private static void BinaryPrimitives_WriteUInt16LE(Span<byte> dest, ushort value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(dest, value);
}
