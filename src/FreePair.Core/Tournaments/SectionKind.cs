namespace FreePair.Core.Tournaments;

/// <summary>
/// Kind of pairing system driving a tournament section. Mapped from the raw
/// SwissSys <c>Type</c> field; unrecognized values become <see cref="Unknown"/>.
/// </summary>
public enum SectionKind
{
    /// <summary>Unknown / unmapped section type.</summary>
    Unknown = -1,

    /// <summary>Swiss-system (default).</summary>
    Swiss = 0,
}
