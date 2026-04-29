# tools/

Per-developer binaries that ship inside the FreePair installer. Gitignored
to keep the repo small and to avoid a license-redistribution decision in
git history; the release pipeline picks them up at pack time.

## bbpPairings.exe

FreePair drives FIDE Dutch Swiss pairings via the BBP Pairings engine
(<https://github.com/BieremaBoyzProgramming/bbpPairings>). The release
installer bundles `bbpPairings.exe` next to `FreePair.App.exe` so end-user
TDs don't have to download / configure it manually.

### One-time setup (per developer / CI host)

1. Download the latest `bbpPairings-windows.zip` from
   <https://github.com/BieremaBoyzProgramming/bbpPairings/releases>.
2. Extract `bbpPairings.exe` into this folder.
3. The `FreePair.App.csproj` `<Content>` item picks it up automatically
   the next time you build/publish (`Condition="Exists(...)"` keeps the
   build green if the file is missing).

### Why gitignored

`bbpPairings` is GPL-licensed. Bundling the unmodified binary in our
installer is fine — we ship a notice in `docs/THIRD_PARTY_NOTICES.md` and
point users at the upstream source. Checking the binary into git history
would commit FreePair to redistributing every revision we ever bundled,
which complicates upgrades. Pulling at build time keeps the rights
question local to each release.

### Manual fallback

If a TD installs FreePair without the bundled exe (e.g. they copied the
folder from somewhere weird), the engine still respects the explicit path
in `Settings → Pairing engine binary` — see
`BbpPairingEngine.NotConfiguredInstructions`.
