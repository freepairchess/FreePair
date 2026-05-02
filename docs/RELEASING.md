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

> **Heads-up about `vpk`'s runtime**: at the time of writing,
> `vpk` ships against .NET 9. FreePair targets .NET 10 and many dev
> boxes don't have a side-by-side .NET 9 runtime. The release script
> sets `$env:DOTNET_ROLL_FORWARD = "LatestMajor"` automatically so vpk
> runs on whatever major you have installed (.NET 10 in our case). If
> you ever invoke `vpk` directly outside the script and see *"You must
> install or update .NET to run this application"*, run
> `$env:DOTNET_ROLL_FORWARD = "LatestMajor"` first.

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
| `FreePair-win-x64-Setup.exe` | The single-file installer you send to TDs. ~90 MB. |
| `FreePair-win-x64-Portable.zip` | Portable zip — extract anywhere, no install. Useful for TDs who can't run installers. |
| `FreePair-<version>-win-x64-full.nupkg` | Velopack full package. Upload alongside Setup so future installs can delta against it. |
| `RELEASES-win-x64` / `releases.win-x64.json` | Velopack manifest + feed metadata. Required for auto-update to work. |
| `assets.win-x64.json` | Velopack metadata. |

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

### Publishing to GitHub Releases (automated)

Once you've smoke-tested, push the build to GitHub Releases with a
single command:

```powershell
# First time: pass -Repo so publish-github.ps1 caches it in
# release\github-repo.txt for subsequent runs.
.\release\publish-github.ps1 -Version 0.1.0 -Repo myorg/FreePair -PreRelease -Draft

# Or do build + publish in one step:
.\release\release.ps1 -Version 0.1.0 -PublishGitHub -GitHubRepo myorg/FreePair -PreRelease -Draft
```

What the publish step does:

1. **Pre-flight**: verifies the [`gh` CLI](https://cli.github.com/) is
   installed and authenticated; if not, prints exact commands to fix
   it and exits.
2. **Resolves the target repo**: command-line `-Repo` →
   `release\github-repo.txt` → URL of the `github` git remote, in
   that order. Caches the resolved value to `github-repo.txt`
   (gitignored) on first run.
3. **Prepares a staging clone of the GitHub repo** at
   `release\github-staging\<owner>-<repo>\` (gitignored). The
   staging dir has its own git history, separate from the source
   repo — see [the two-repo model](#source-isolation-the-two-repo-model)
   below for why.
4. **Writes a release-notes marker** to
   `release-notes\v<Version>.md` in the staging clone, commits it
   on the staging repo's `main` branch, and tags that commit
   `v<Version>`. The tag points at the marker commit, NOT at any
   FreePair source commit.
5. **Pushes `main` + tag** to the GitHub repo via the staging
   clone's `origin` remote. The source repo's working tree and its
   Azure DevOps `origin` remote are never touched.
6. **Creates the release**: calls `gh release create v<Version>`
   with the title, optional release-notes file, and every file in
   `release\output\<Version>\` as an attached asset.
7. If the release already exists (re-cut of an existing version),
   re-uploads assets via `gh release upload --clobber`.

#### Source isolation: the two-repo model

FreePair source lives **only** on Azure DevOps
(`xuhaohe.visualstudio.com/FreePair`, private). The GitHub repo
(`freepairchess/FreePair`, public) holds **only**:

- `README.md` (one-line "FreePair binaries; source private")
- `release-notes\v0.1.0.md`, `release-notes\v0.1.1.md`, … (per-release Markdown markers)
- Tags `v0.1.0`, `v0.1.1`, … pointing at marker commits
- GitHub Releases with the binary assets (Setup.exe, Portable.zip, nupkgs, manifests)

Why a staging clone instead of pushing tags from the source repo:
`git push <remote> v<Version>` pushes the tag **and every commit
reachable from it**, which would drag the entire FreePair source
history onto GitHub. The staging clone has its own minimal history
(just the per-release markers), so even though the tag-push still
includes everything reachable, "everything reachable" is only ever
the markdown markers. Source can never leak.

The staging clone is gitignored (`release\github-staging\` in
`release\.gitignore`) so it doesn't pollute the source repo. First
run clones the GitHub repo automatically; subsequent runs `git
fetch` + reset to `origin/main` so the local state always matches
GitHub before tagging.

#### Credential management

Authentication runs through the `gh` CLI. One-time setup:

```powershell
winget install --id GitHub.cli   # or choco / scoop, your pick
gh auth login                     # browser-based; stores creds in Windows Credential Manager
```

After that, the script never sees a token — `gh` reads from the OS
keyring on each invocation. Re-running on a different machine just
needs another `gh auth login`. There's no PAT to commit, rotate, or
leak.

#### Multiple GitHub accounts on one machine

If you have **more than one** `gh auth login` identity on the same
machine (typical: a personal Copilot account + a separate
FreePair-publisher account), pass `-ExpectedUser <username>` (or
`-GitHubUser` to `release.ps1`) so the publisher refuses to run
under the wrong account:

```powershell
.\release\release.ps1 `
    -Version 0.1.0 -PublishGitHub `
    -GitHubRepo freepairchess/FreePair `
    -GitHubUser <freepair-publisher-username> `
    -PreRelease -Draft
```

The script:

1. Reads the active gh user via `gh api user --jq .login`.
2. If it doesn't match `-ExpectedUser`, prints `gh auth switch -h github.com -u <username>` and exits before doing anything destructive.
3. Caches the expected user to `release\github-user.txt` so subsequent runs are zero-arg.

To switch accounts manually (no script involvement):

```powershell
gh auth status                                     # see all logged-in accounts
gh auth switch -h github.com -u <freepair-user>    # make FreePair account active
gh auth switch -h github.com -u <copilot-user>     # switch back when done
```

`gh auth switch` only changes which account `gh` uses; nothing else
on the machine is affected. The Copilot extension in your editor
keeps working because it has its own auth session independent of the
`gh` CLI.

#### Useful flags

| Flag | What it does |
|---|---|
| `-PreRelease` | Marks the GitHub release as a pre-release (keeps it out of `/releases/latest` until you flip the flag). Use until you're confident in the build. |
| `-Draft` | Creates as draft — open the release URL and click *Publish release* manually. Recommended for the first run. |
| `-ReleaseNotes <path.md>` | Body of the release. Markdown rendered by GitHub. |
| `-GenerateNotes` | Asks GitHub to auto-generate notes from the commit list since the previous tag. Layered on top of `-ReleaseNotes` if both are passed. |
| `-SkipTagPush` | Skip the `git push` of the tag. Useful when the tag is already on the remote. |

#### What TDs see

The TD-facing direct download URL is stable:

```
https://github.com/<owner>/FreePair/releases/latest/download/FreePair-win-x64-Setup.exe
```

(GitHub redirects to whichever release is marked "latest" — pre-release
and draft releases are skipped by `/latest`.)

### Manual fallback (rare)

If `gh` is unavailable, fall back to the GitHub web UI:

1. **GitHub Releases** — tag `v<version>` and upload every file in
   `release\output\<version>\`. Mark as a pre-release for early
   versions until you're confident in the auto-update flow.
2. **Direct link** — point TDs at the latest release URL:
   `https://github.com/<owner>/FreePair/releases/latest/download/FreePair-win-x64-Setup.exe`.
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
pack` → `gh release create` dance from inside the runner instead of
the developer's machine. Removes the "did I forget a step?" risk and
lets non-developers cut releases by tagging. The current
`publish-github.ps1` is the basis — it works unchanged inside an
Actions runner that has `gh` pre-installed; the only swap-out is
authentication (`GITHUB_TOKEN` env var instead of `gh auth login`).

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
That handles the install / uninstall / first-run hooks.

**In-app update flow (TD-facing)**:

- `MainWindow` constructs a `VelopackUpdateService` against the URL in
  `AppSettings.UpdateFeedRepoUrl` (default
  `https://github.com/freepairchess/FreePair`) and kicks off a
  fire-and-forget `CheckForUpdatesAsync` when
  `AppSettings.CheckForUpdatesOnStartup` is on (default).
- When the check finds a newer release, the main window shows a
  yellow banner: **"Update available: FreePair v0.2.0 — save your
  work first"** with a **View notes** button (tooltips the Markdown
  release body) and an **Update now** button (calls
  `UpdateManager.DownloadUpdatesAsync` + `ApplyUpdatesAndRestart`,
  exiting FreePair so Velopack's launcher can swap binaries).
- Settings → Auto-update lets TDs disable the startup check, opt
  into pre-release builds, or override the feed URL (forks).
- TDs running outside an installed Velopack package (dev /
  `dotnet run` / portable extract) never see the banner —
  `VelopackUpdateService` returns `NotInstalled` and the VM
  silently skips it.

**What a TD on v0.1.0 sees when v0.2.0 is published with `--latest`**:

1. They open FreePair as usual.
2. Within ~2 seconds the yellow banner appears at the top of the
   main window. (The check is anonymous against the GitHub API;
   no rate-limit issues for the first ~60 launches/hour.)
3. They click **Update now**.
4. FreePair downloads the delta nupkg (~1–5 MB depending on what
   changed), applies it, and restarts into v0.2.0.

If they prefer not to auto-update, they can always download the
fresh `Setup.exe` from the GitHub release page and run it — the
installer detects the existing install at `%LocalAppData%\FreePair\`
and upgrades in place. Settings, tournament files, and the `gh`
cred store are untouched either way.
