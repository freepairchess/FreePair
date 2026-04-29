# Releasing FreePair

End-to-end recipe for cutting a FreePair installer for Windows TDs. The
release pipeline ships a single self-contained `Setup.exe` that includes
the .NET 10 runtime, the Avalonia UI assets, and the bundled
`bbpPairings.exe`, so TDs install one file and never have to fiddle with
runtimes or pairing engines.

## Phase 1 (current) — unsigned, manual GitHub Releases

We're at "ship to a few friendly TDs" stage. Code-signing comes later
(see [Phase 3](#phase-3-code-signing-tldr-trusted-signing) below) — until
then, end users will see a SmartScreen "unrecognized app" warning on
first install and have to click **More info → Run anyway**. Document
this in your "how to install" message.

### One-time per-developer setup

1. **Install the .NET 10 SDK** (build will already complain if missing).
2. **Download `bbpPairings.exe`** into `tools\bbpPairings.exe`. The
   `tools\README.md` has the link and rationale; the `tools\.gitignore`
   keeps the binary out of git.
3. **Install the Velopack global tool** the first time you cut a release:
   the script does this automatically, but you can pre-warm it with
   `dotnet tool install -g vpk`.

### Cutting a release

```powershell
# from the repo root
.\release\release.ps1 -Version 0.1.0
```

The script will:

1. Verify `tools\bbpPairings.exe` is present.
2. Run the full unit-test suite (currently 440 tests). Aborts on red.
3. `dotnet publish -c Release -r win-x64 --self-contained` — single-
   file, ReadyToRun, no PDBs.
4. `vpk pack` to produce a Velopack installer + full/delta nupkgs.

Output lands in `release\output\<version>\`:

| File | Purpose |
|---|---|
| `FreePair-win-Setup.exe` | The single-file installer you send to TDs. ~80 MB. |
| `FreePair-<version>-full.nupkg` | Velopack package (full release). Upload alongside Setup so future installs can delta against it. |
| `FreePair-<version>-delta.nupkg` | Created from the 2nd release on; tiny binary diff. |
| `RELEASES` | Velopack manifest. Required for auto-update to work. |

### Smoke-testing before publish

On a clean Windows VM that **doesn't** already have FreePair or .NET 10:

1. Run `FreePair-win-Setup.exe`. It should install silently within a
   few seconds and launch FreePair.
2. Open a `.sjson` file (any sample under `tests\FreePair.Core.Tests\Data\`).
3. Click **Generate next round** on a section. If the bundled
   `bbpPairings.exe` is wired correctly, pairings appear without any
   "Pairing engine not configured" banner.
4. Verify **Help → About** (or the title bar) shows the version you cut.
5. Check **Add or remove programs**: a "FreePair" entry with the right
   version should appear, with a working uninstaller.

### Publishing to TDs

1. **GitHub Releases** — tag `v<version>` and upload every file in
   `release\output\<version>\`. Mark as a pre-release for early
   versions until you're confident in the auto-update flow.
2. **Direct link** — point TDs at the latest release URL:
   `https://github.com/<owner>/FreePair/releases/latest/download/FreePair-win-Setup.exe`.
3. **Release notes** — pass `-ReleaseNotes` to the script with a
   markdown file. End users see this in the Velopack updater UI.

### What TDs need to do

Just download `FreePair-win-Setup.exe` and run it. The installer drops
FreePair into `%LocalAppData%\FreePair\current\`, creates a Start Menu
entry, and registers an uninstaller. No admin rights required (per-user
install). On first run they'll likely see SmartScreen — they should
click **More info → Run anyway** until we ship Phase 3.

## Phase 2 (future) — automated CI releases

Move the script into a GitHub Actions workflow triggered on `v*` tag
pushes. Workflow does the same `dotnet test` → `dotnet publish` → `vpk
pack` dance, then uploads via the `softprops/action-gh-release` action.
Removes the "did I forget a step?" risk and lets non-developers cut
releases by tagging.

## Phase 3 — code signing (TL;DR Trusted Signing)

Without a signed `Setup.exe`, every TD sees the SmartScreen warning.
Three options, in increasing cost / decreasing friction for end users:

| Option | Cost | Setup pain | SmartScreen behaviour |
|---|---|---|---|
| Unsigned (Phase 1) | $0 | none | Always warns until the binary builds reputation (~3000 downloads) |
| **Microsoft Trusted Signing** | ~$10/mo | Azure subscription + identity verification | Reputation builds normally, no hardware token, signs in CI |
| EV cert (DigiCert/Sectigo) | $300–700/yr | Hardware token (USB / cloud HSM) required since Nov 2023 | Instant SmartScreen reputation from the first download |

Recommended path: when the user count justifies it, switch to **Trusted
Signing**. Add a `--signParams` argument to the `vpk pack` invocation in
`release.ps1` that points at the signing config — Velopack signs both
the inner `FreePair.App.exe` and the outer `Setup.exe` automatically.

## Auto-update (already wired)

`Program.cs` calls `VelopackApp.Build().Run()` before Avalonia spins up.
That handles the install / uninstall / first-run hooks. To check for
updates from inside a running FreePair instance you can later add a
"Help → Check for updates" menu item that calls `UpdateManager.UpdateAndRestart()`
against the GitHub Releases feed. Out of scope for Phase 1, but the
plumbing is in place.
