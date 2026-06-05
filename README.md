# FreePair

**Free, open-source chess tournament pairing for the desktop.**

FreePair is a cross-platform tournament-director tool for chess tournaments. It imports SwissSys `.sjson` files, runs USCF and FIDE pairings, scores results, prints wall charts and pairings as PDFs, and lets the TD intervene at any board. Designed for USCF-rated events.

This GitHub repo is the **public download channel** for the Windows installer. Source code lives in a separate private development repo.

## Download

The current Windows installer is always available at:

- **Latest version:** [FreePair-win-x64-Setup.exe](https://github.com/freepairchess/FreePair/releases/latest/download/FreePair-win-x64-Setup.exe)
- **Portable zip:** [FreePair-win-x64-Portable.zip](https://github.com/freepairchess/FreePair/releases/latest/download/FreePair-win-x64-Portable.zip)
- **All versions:** [Releases page](https://github.com/freepairchess/FreePair/releases)

After install, FreePair auto-updates from new releases here — you don't need to download manually.

## What's in this repo

| Path | Purpose |
|---|---|
| [docs/USCF_RULES.md](docs/USCF_RULES.md) | The authoritative USCF pairing rules reference the app's "Why this pairing?" dialog links to. ~95 rules covering chapters 22, 27, 28, 29 of the USCF *Official Rules of Chess* (7th ed.). |
| [release-notes/](release-notes/) | One markdown file per release describing what shipped. |
| `Releases` (the GitHub tab) | The actual installer + portable zip + Velopack manifests for each version. |

Source code and issue tracker are not hosted here. For bug reports or feature requests, please contact the maintainer directly until a public issue tracker is announced.

## License

To be announced. The installer is free to download and use; bundled third-party components retain their own licenses (Avalonia UI — MIT; BBP Pairings — Apache 2.0; .NET Runtime — MIT).
