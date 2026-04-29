# Third-party notices

FreePair is distributed under [its own license](../LICENSE). The shipped
installer bundles the following third-party software, redistributed
under their respective licenses:

## bbpPairings

- **Project**: BBP Pairings
- **Source**: <https://github.com/BieremaBoyzProgramming/bbpPairings>
- **License**: Apache-2.0 (see the upstream `LICENSE` file)
- **What we ship**: an unmodified `bbpPairings.exe` next to
  `FreePair.App.exe` so FreePair can drive FIDE Dutch Swiss pairings
  out-of-the-box.
- **Source availability**: per the project's terms, the upstream repo
  link above is the canonical source. Contact us if you need the
  specific revision a given FreePair release bundled.

## .NET runtime + Avalonia + dependencies

The single-file installer also embeds the .NET 10 self-contained runtime
and the Avalonia UI framework. License notices for those components are
covered by their respective NuGet package metadata, included in the
publish output under each runtime DLL's manifest.
