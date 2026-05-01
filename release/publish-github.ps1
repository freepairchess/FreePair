<#
.SYNOPSIS
    Publishes a built FreePair release to GitHub Releases — creates the
    git tag, pushes it, opens a Release, and uploads every artefact in
    release\output\<version>\.

.DESCRIPTION
    Companion to release\release.ps1. Run release.ps1 first to produce
    the installer + nupkgs in release\output\<Version>\, then run this
    script (or pass -PublishGitHub to release.ps1 to do it all in one
    step) to push everything to GitHub.

    Authentication is delegated to the GitHub CLI (`gh`), which stores
    credentials in the OS keyring after a one-time `gh auth login` —
    the script never sees a PAT in plaintext. If `gh` isn't installed
    or the TD isn't logged in, the script prints exact remediation
    commands and exits cleanly.

.PARAMETER Version
    SemVer string (no leading 'v'). Must match a folder under
    release\output\.

.PARAMETER Repo
    GitHub repository in '<owner>/<repo>' form. Optional — the script
    will read release\github-repo.txt if present, then fall back to
    the URL of the 'github' git remote. Stored in
    release\github-repo.txt on first run so subsequent invocations
    don't need this argument.

.PARAMETER Remote
    Git remote name to push the tag to. Defaults to 'github' if such
    a remote exists, otherwise 'origin'. The remote must point at the
    GitHub repo (the script verifies this).

.PARAMETER ReleaseNotes
    Optional path to a Markdown file describing what changed. Body of
    the GitHub release.

.PARAMETER Title
    Release title. Defaults to "FreePair v<Version>".

.PARAMETER PreRelease
    Mark the release as a pre-release on GitHub (recommended for early
    builds — keeps it out of "latest" until you flip the flag).

.PARAMETER Draft
    Create the release as a draft so you can review the asset list and
    notes before publishing. Recommended on first runs.

.PARAMETER SkipTagPush
    Skip the `git push` of the tag. Useful when the tag already exists
    on the GitHub remote and you only want to (re)create the release.

.PARAMETER GenerateNotes
    Tells GitHub to auto-generate release notes from the commit list
    since the previous tag. Layered on top of -ReleaseNotes if both
    are passed.

.PARAMETER ExpectedUser
    GitHub username the script must be authenticated as before doing
    anything destructive (tagging, pushing, creating releases). Used
    to prevent cross-account contamination when one machine has
    multiple `gh auth login` identities (e.g. a personal Copilot
    account + a separate FreePair-publisher account). On mismatch
    the script prints the exact `gh auth switch` command and exits.
    Cached in release\github-user.txt on first use so subsequent
    runs are zero-arg.

.EXAMPLE
    # First-time: tell the script which repo to publish to. The repo is
    # remembered in release\github-repo.txt for future runs.
    .\release\publish-github.ps1 -Version 0.2.0 -Repo myorg/FreePair -PreRelease -Draft

.EXAMPLE
    # Subsequent runs — repo + remote auto-detected.
    .\release\publish-github.ps1 -Version 0.3.0 -ReleaseNotes .\docs\release-notes\0.3.0.md -GenerateNotes

.EXAMPLE
    # Multi-account safety: the script verifies the active gh user is
    # the FreePair publisher before doing anything. ExpectedUser is
    # cached after the first run.
    .\release\publish-github.ps1 -Version 0.4.0 -ExpectedUser freepair-publisher

.NOTES
    The Azure DevOps origin remote is left untouched. We only push the
    release tag to the github remote so the .sjson + Avalonia source
    can stay primary on Azure DevOps while binaries ship via GitHub.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [string] $Repo,
    [string] $Remote,
    [string] $ReleaseNotes,
    [string] $Title,
    [string] $ExpectedUser,
    [switch] $PreRelease,
    [switch] $Draft,
    [switch] $SkipTagPush,
    [switch] $GenerateNotes
)

$ErrorActionPreference = "Stop"
$ProgressPreference   = "SilentlyContinue"

$repoRoot   = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputDir  = Join-Path $repoRoot "release\output\$Version"
$repoFile   = Join-Path $repoRoot "release\github-repo.txt"
$userFile   = Join-Path $repoRoot "release\github-user.txt"
$tag        = "v$Version"

Write-Host "==> Publishing FreePair $tag to GitHub" -ForegroundColor Cyan

# 1. Pre-flight: gh CLI installed --------------------------------------
if (-not (Get-Command gh -ErrorAction SilentlyContinue))
{
    Write-Host ""
    Write-Error @"
GitHub CLI ('gh') is not installed.

Install with one of:
    winget install --id GitHub.cli
    choco install gh
    scoop install gh

Then run 'gh auth login' once to authenticate (browser-based).
Re-run this script after.
"@
    exit 1
}

# 2. Pre-flight: gh authenticated --------------------------------------
& gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0)
{
    Write-Host ""
    Write-Error @"
GitHub CLI is installed but not authenticated. Run:

    gh auth login

(choose HTTPS + browser-based login when prompted; it stores creds in
the Windows Credential Manager so this script never sees a token)

Then re-run this script.
"@
    exit 1
}

# 2b. Multi-account safety: verify the active gh user matches the
# FreePair publisher identity. Prevents accidentally publishing from
# a Copilot / personal / day-job account that also happens to have
# `gh auth login` on this machine.
if (-not $ExpectedUser -and (Test-Path $userFile))
{
    $ExpectedUser = (Get-Content $userFile -Raw).Trim()
    if ($ExpectedUser) {
        Write-Host "    Expected gh user (from release\github-user.txt): $ExpectedUser"
    }
}
$activeUser = (& gh api user --jq .login 2>$null).Trim()
if (-not $activeUser)
{
    Write-Error "Could not determine the active gh user. Run 'gh auth status' to investigate."
    exit 1
}
Write-Host "    Active gh user: $activeUser"

if ($ExpectedUser -and ($activeUser -ne $ExpectedUser))
{
    Write-Host ""
    Write-Error @"
Active gh user is '$activeUser' but the FreePair publisher should be
'$ExpectedUser'. Switch with:

    gh auth switch --hostname github.com --user $ExpectedUser

If '$ExpectedUser' isn't yet logged in on this machine:

    gh auth login --hostname github.com --git-protocol https --web

(then run 'gh auth switch ...' again to make $ExpectedUser the active
account before re-running this script)

Refusing to continue rather than risk publishing from the wrong account.
"@
    exit 1
}

# Persist ExpectedUser so subsequent runs auto-verify even if the
# argument is omitted. First successful run sets the value.
if ($ExpectedUser -and (-not (Test-Path $userFile) -or ((Get-Content $userFile -Raw).Trim() -ne $ExpectedUser)))
{
    Set-Content -Path $userFile -Value $ExpectedUser -NoNewline
    Write-Host "    Cached '$ExpectedUser' in release\github-user.txt"
}
elseif (-not $ExpectedUser)
{
    Write-Warning @"
No -ExpectedUser argument and no release\github-user.txt cache. Continuing
with active user '$activeUser', but consider passing -ExpectedUser once
to lock in the publisher identity for future runs.
"@
}

# 3. Resolve the target repo (param > config file > github remote) ------
if (-not $Repo)
{
    if (Test-Path $repoFile)
    {
        $Repo = (Get-Content $repoFile -Raw).Trim()
        if ($Repo) {
            Write-Host "    Repo (from release\github-repo.txt): $Repo"
        }
    }
}
if (-not $Repo)
{
    # Try to derive from a 'github' git remote URL.
    $githubRemoteUrl = & git -C $repoRoot remote get-url github 2>$null
    if ($githubRemoteUrl -and $githubRemoteUrl -match "github\.com[:/](.+?)(\.git)?$")
    {
        $Repo = $Matches[1]
        Write-Host "    Repo (from 'github' git remote): $Repo"
    }
}
if (-not $Repo)
{
    Write-Error @"
Couldn't determine the target GitHub repo. Pass -Repo '<owner>/<repo>'
once; the value is cached in release\github-repo.txt for future runs.

Example:
    .\release\publish-github.ps1 -Version $Version -Repo myorg/FreePair
"@
    exit 1
}

# Persist the resolved repo so the next run is one less argument.
if (-not (Test-Path $repoFile) -or ((Get-Content $repoFile -Raw).Trim() -ne $Repo))
{
    Set-Content -Path $repoFile -Value $Repo -NoNewline
    Write-Host "    Cached '$Repo' in release\github-repo.txt"
}

# 4. Resolve the git remote we'll push the tag to ----------------------
if (-not $Remote)
{
    $hasGithub = & git -C $repoRoot remote 2>$null | Select-String -SimpleMatch "github"
    $Remote = $hasGithub ? "github" : "origin"
}

# Verify the remote points at the GitHub repo we just resolved.
if (-not $SkipTagPush)
{
    $remoteUrl = & git -C $repoRoot remote get-url $Remote 2>$null
    if (-not $remoteUrl)
    {
        Write-Error @"
Git remote '$Remote' is not configured. Add it with:

    git remote add $Remote https://github.com/$Repo.git

Or pass -SkipTagPush if you've pushed the tag to GitHub through some
other means.
"@
        exit 1
    }
    if ($remoteUrl -notmatch "github\.com[:/]$([regex]::Escape($Repo))(\.git)?$")
    {
        Write-Warning "Remote '$Remote' URL ($remoteUrl) doesn't obviously match $Repo. Continuing anyway."
    }
}

# 5. Pre-flight: artefacts exist ---------------------------------------
if (-not (Test-Path $outputDir))
{
    Write-Error @"
No build output for $Version at:

    $outputDir

Run release\release.ps1 -Version $Version first to produce the
installer + nupkgs.
"@
    exit 1
}
$assets = Get-ChildItem -Path $outputDir -File
if ($assets.Count -eq 0)
{
    Write-Error "Output folder is empty: $outputDir"
    exit 1
}

# 5b. Pre-flight: -ReleaseNotes file exists -----------------------------
# Validated up-front so we never push a tag and then bail on a missing
# Markdown file (which leaves a tag on the remote with no GitHub
# release attached — the awkward state the previous run hit).
if ($ReleaseNotes -and -not (Test-Path $ReleaseNotes))
{
    Write-Error @"
-ReleaseNotes path does not exist:

    $ReleaseNotes

Pick one:
  * Create the file (Markdown body of the GitHub release).
  * Re-run with -GenerateNotes to auto-generate from commit messages
    since the previous tag.
  * Omit -ReleaseNotes entirely; the script falls back to a one-line
    stub you can edit later in the GitHub UI.

(Refusing to continue: this check fires BEFORE tag/push so a typo in
the path can't leave a tag on the remote with no release attached.)
"@
    exit 1
}

Write-Host ""
Write-Host "Assets to upload ($($assets.Count) file(s)):"
$assets | ForEach-Object {
    $sizeMB = [Math]::Round($_.Length / 1MB, 2)
    Write-Host ("    {0}   ({1} MB)" -f $_.Name, $sizeMB)
}

# 6. Tag the current commit (if needed) and push it --------------------
$tagExistsLocal = (& git -C $repoRoot tag --list $tag) -ne $null -and (& git -C $repoRoot tag --list $tag) -ne ""
if (-not $tagExistsLocal)
{
    Write-Host ""
    Write-Host "==> Creating annotated tag $tag" -ForegroundColor Cyan
    & git -C $repoRoot tag -a $tag -m "Release $tag"
    if ($LASTEXITCODE -ne 0) { throw "git tag failed." }
}
else
{
    Write-Host "    Tag $tag already exists locally."
}

if (-not $SkipTagPush)
{
    Write-Host "==> Pushing tag $tag to '$Remote'" -ForegroundColor Cyan
    & git -C $repoRoot push $Remote $tag 2>&1 | Tee-Object -Variable pushOut | Out-Host
    if ($LASTEXITCODE -ne 0)
    {
        # If the tag is already on the remote, gh release create still
        # works against it — surface a friendlier message.
        if ($pushOut -match "already exists" -or $pushOut -match "rejected")
        {
            Write-Warning "Tag $tag already on remote '$Remote'; continuing."
        }
        else
        {
            throw "git push of tag failed."
        }
    }
}

# 7. Build the gh release create invocation ----------------------------
if (-not $Title) { $Title = "FreePair $tag" }

$ghArgs = @(
    "release", "create", $tag
    "--repo",  $Repo
    "--title", $Title
)
if ($PreRelease)    { $ghArgs += "--prerelease" }
if ($Draft)         { $ghArgs += "--draft" }
if ($GenerateNotes) { $ghArgs += "--generate-notes" }

if ($ReleaseNotes)
{
    # Existence already validated in the pre-flight block above; the
    # Resolve-Path is just to give gh a normalised absolute path.
    $ghArgs += @("--notes-file", (Resolve-Path $ReleaseNotes))
}
elseif (-not $GenerateNotes)
{
    # gh requires SOME notes source — fall back to a one-line stub so
    # the release isn't blocked. TDs editing in the GitHub UI later is
    # fine; -GenerateNotes is the better default for non-trivial events.
    $ghArgs += @("--notes", "Release $tag — see release artefacts below.")
}

# Asset list comes after the flag args.
$ghArgs += $assets.FullName

# 8. Create / replace the release -------------------------------------
Write-Host ""
Write-Host "==> gh release create $tag --repo $Repo" -ForegroundColor Cyan

# Detect existing release: gh release view returns nonzero on miss.
& gh release view $tag --repo $Repo 2>&1 | Out-Null
$releaseExists = ($LASTEXITCODE -eq 0)

if ($releaseExists)
{
    Write-Warning "Release $tag already exists on GitHub. Re-uploading assets via 'gh release upload --clobber'."
    $uploadArgs = @("release", "upload", $tag, "--repo", $Repo, "--clobber") + $assets.FullName
    & gh @uploadArgs
    if ($LASTEXITCODE -ne 0) { throw "gh release upload failed." }
}
else
{
    & gh @ghArgs
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed." }
}

# 9. Print the public URL ---------------------------------------------
$releaseUrl = & gh release view $tag --repo $Repo --json url --jq .url 2>$null
Write-Host ""
Write-Host "==> Done." -ForegroundColor Green
Write-Host "    Release: $releaseUrl"
Write-Host ""
Write-Host "TD-facing direct download for the latest stable build:"
Write-Host "    https://github.com/$Repo/releases/latest/download/FreePair-win-x64-Setup.exe"
if ($Draft)
{
    Write-Host ""
    Write-Host "(Release was created as a DRAFT — open the URL above and click 'Publish release' when ready.)" -ForegroundColor Yellow
}
