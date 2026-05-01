<#
.SYNOPSIS
    Builds a FreePair Windows installer + delta package via Velopack.

.DESCRIPTION
    Wraps `dotnet publish` + `vpk pack` for a single release. Output goes
    into release\output\<version>\:
        FreePair-win-Setup.exe         <- single-file user-facing installer
        FreePair-<version>-full.nupkg  <- Velopack full package
        FreePair-<version>-delta.nupkg <- Velopack delta (created from 2nd release on)
        RELEASES                       <- Velopack manifest

    The installer auto-creates a Start Menu entry, an uninstaller in
    Add/Remove Programs, and (when wired up to a feed) handles auto-update
    via the Velopack runtime baked into FreePair.App.exe.

.PARAMETER Version
    SemVer string. Pass without a leading 'v'. Required.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER ReleaseNotes
    Optional path to a Markdown file describing what changed in this
    release; embedded into the Velopack package.

.PARAMETER PublishGitHub
    After a successful build, hand off to release\publish-github.ps1 to
    tag the commit, push the tag, and create a GitHub Release with all
    artefacts uploaded. Requires a one-time `gh auth login` and a
    cached or -Repo-supplied <owner>/<repo>. See publish-github.ps1
    for full options.

.PARAMETER GitHubRepo
    GitHub <owner>/<repo> for -PublishGitHub. Optional after the first
    run (publish-github.ps1 caches the value in release\github-repo.txt).

.PARAMETER GitHubUser
    GitHub username the publisher MUST be authenticated as before
    pushing tags / creating releases. Forwarded to publish-github.ps1
    as -ExpectedUser. Cached in release\github-user.txt after first
    use so subsequent runs are zero-arg. Use this when the same
    machine has multiple `gh auth login` identities (e.g. a Copilot
    account + a separate FreePair-publisher account) and you don't
    want to risk picking the wrong one.

.PARAMETER PreRelease
    Forwarded to publish-github.ps1: marks the GitHub release as a
    pre-release. Recommended for early builds.

.PARAMETER Draft
    Forwarded to publish-github.ps1: creates the GitHub release as a
    draft so you can review assets + notes before publishing.

.PARAMETER GenerateNotes
    Forwarded to publish-github.ps1: tells GitHub to auto-generate
    release notes from the commit list since the previous tag.
    Layered on top of -ReleaseNotes if both are passed; either alone
    is fine.

.PARAMETER SkipTagPush
    Forwarded to publish-github.ps1: skip the `git push` of the tag.
    Useful when the tag is already on the GitHub remote and you only
    want to (re)create the release attached to it.

.PARAMETER GitHubReleaseTitle
    Forwarded to publish-github.ps1 as -Title. Defaults to
    'FreePair v<Version>' if omitted.

.EXAMPLE
    .\release\release.ps1 -Version 0.1.0
    .\release\release.ps1 -Version 0.2.0 -ReleaseNotes .\docs\release-notes\0.2.0.md
    .\release\release.ps1 -Version 0.3.0 -PublishGitHub -GitHubRepo myorg/FreePair -PreRelease -Draft
    .\release\release.ps1 -Version 0.4.0 -PublishGitHub -GenerateNotes -PreRelease -Draft
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [string] $Configuration = "Release",
    [string] $ReleaseNotes,
    [switch] $PublishGitHub,
    [string] $GitHubRepo,
    [string] $GitHubUser,
    [string] $GitHubReleaseTitle,
    [switch] $PreRelease,
    [switch] $Draft,
    [switch] $GenerateNotes,
    [switch] $SkipTagPush
)

$ErrorActionPreference = "Stop"
$ProgressPreference   = "SilentlyContinue"

# vpk (Velopack) is published against .NET 9 — but FreePair targets
# .NET 10 and many dev boxes don't have a side-by-side .NET 9 runtime.
# DOTNET_ROLL_FORWARD lets vpk run on the latest installed major
# (typically .NET 10 here) without forcing every contributor to
# install a 9.x runtime just to ship a release.
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

$repoRoot   = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProj    = Join-Path $repoRoot "src\FreePair.App\FreePair.App.csproj"
$bbpExe     = Join-Path $repoRoot "tools\bbpPairings.exe"
$publishDir = Join-Path $repoRoot "release\publish"
$outputDir  = Join-Path $repoRoot "release\output\$Version"
$rid        = "win-x64"

Write-Host "==> FreePair release $Version  ($Configuration / $rid)" -ForegroundColor Cyan

# Sanity checks -----------------------------------------------------------
if (-not (Test-Path $bbpExe))
{
    throw "Bundled bbpPairings binary missing at '$bbpExe'. " `
        + "See tools\README.md for the one-time download step."
}

# vpk global tool. Install if absent.
if (-not (Get-Command vpk -ErrorAction SilentlyContinue))
{
    Write-Host "==> Installing Velopack tool (vpk) ..." -ForegroundColor Yellow
    dotnet tool install -g vpk
    if ($LASTEXITCODE -ne 0) { throw "Failed to install vpk." }
}

# Clean previous artefacts ------------------------------------------------
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $outputDir)  { Remove-Item -Recurse -Force $outputDir  }
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $outputDir  | Out-Null

# Run all unit tests before producing a release ---------------------------
Write-Host "==> Running unit tests ..." -ForegroundColor Cyan
dotnet test (Join-Path $repoRoot "tests\FreePair.Core.Tests\FreePair.Core.Tests.csproj") `
    --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Tests failed; aborting release." }

# Publish self-contained, single-file --------------------------------------
# - SelfContained=true bakes the .NET 10 runtime into the publish folder
#   so TDs don't have to install the runtime separately.
# - PublishSingleFile collapses managed DLLs into one exe; native deps
#   (sqlite, skia, etc.) stay alongside.
# - PublishReadyToRun ahead-of-time-compiles the IL so cold start is
#   snappier on lower-end laptops typical at chess clubs.
# - Trim is intentionally OFF: Avalonia + reflection-heavy bindings
#   regressed when trimmed in 12.x; cost a few extra MB to keep stable.
Write-Host "==> dotnet publish ..." -ForegroundColor Cyan
dotnet publish $appProj `
    --configuration $Configuration `
    --runtime $rid `
    --self-contained true `
    --output $publishDir `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:DebugType=None `
    /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Verify the bundled bbpPairings.exe made it into publish output.
$publishedBbp = Join-Path $publishDir "bbpPairings.exe"
if (-not (Test-Path $publishedBbp))
{
    throw "bbpPairings.exe is missing from publish output ($publishedBbp). " `
        + "Check the <Content> item in src\FreePair.App\FreePair.App.csproj."
}

# Velopack pack -----------------------------------------------------------
# - --packId is the per-app identifier; must NOT change between releases.
# - --packVersion drives delta-pack matching against prior releases.
# - --mainExe matches the published exe name (FreePair.App.exe).
# - --packDir is the publish folder Velopack zips up.
# - --outputDir is where vpk writes the installer + nupkgs.
Write-Host "==> vpk pack ..." -ForegroundColor Cyan
$vpkArgs = @(
    "pack"
    "--packId",      "FreePair"
    "--packVersion", $Version
    "--packDir",     $publishDir
    "--mainExe",     "FreePair.App.exe"
    "--packTitle",   "FreePair"
    "--packAuthors", "FreePair contributors"
    "--outputDir",   $outputDir
    "--runtime",     $rid
    "--channel",     $rid
)
if ($ReleaseNotes -and (Test-Path $ReleaseNotes))
{
    $vpkArgs += @("--releaseNotes", (Resolve-Path $ReleaseNotes))
}

vpk @vpkArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

Write-Host ""
Write-Host "==> Done. Artefacts in $outputDir" -ForegroundColor Green
Get-ChildItem $outputDir | Format-Table Name, Length -AutoSize

if ($PublishGitHub)
{
    Write-Host ""
    Write-Host "==> Handing off to publish-github.ps1 ..." -ForegroundColor Cyan
    $publishScript = Join-Path $PSScriptRoot "publish-github.ps1"
    $publishArgs = @{ Version = $Version }
    if ($GitHubRepo)         { $publishArgs.Repo          = $GitHubRepo }
    if ($GitHubUser)         { $publishArgs.ExpectedUser  = $GitHubUser }
    if ($ReleaseNotes)       { $publishArgs.ReleaseNotes  = $ReleaseNotes }
    if ($GitHubReleaseTitle) { $publishArgs.Title         = $GitHubReleaseTitle }
    if ($PreRelease)         { $publishArgs.PreRelease    = $true }
    if ($Draft)              { $publishArgs.Draft         = $true }
    if ($GenerateNotes)      { $publishArgs.GenerateNotes = $true }
    if ($SkipTagPush)        { $publishArgs.SkipTagPush   = $true }
    & $publishScript @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "GitHub publish step failed; build artefacts are still in $outputDir." }
    return
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Smoke-test '$outputDir\FreePair-win-Setup.exe' on a clean Windows VM."
Write-Host "  2. Re-run with -PublishGitHub to tag + upload, OR run release\publish-github.ps1 directly."
Write-Host "  3. Send TDs the direct download link to the Setup.exe."
