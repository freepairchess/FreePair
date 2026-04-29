using System.IO;
using FreePair.Core.Bbp;

namespace FreePair.Core.Tests.Bbp;

/// <summary>
/// Tests <see cref="BbpPairingEngine.ResolveEffectivePath"/> — the
/// helper that lets release builds ship a sibling
/// <c>bbpPairings.exe</c> without forcing the TD to set
/// <see cref="Settings.AppSettings.PairingEngineBinaryPath"/>
/// explicitly.
/// </summary>
public class BbpPairingEnginePathResolutionTests
{
    [Fact]
    public void ResolveEffectivePath_returns_configured_when_it_exists()
    {
        var tempExe = Path.Combine(Path.GetTempPath(), $"fp-bbp-{System.Guid.NewGuid():N}.exe");
        File.WriteAllBytes(tempExe, [0]);
        try
        {
            Assert.Equal(tempExe, BbpPairingEngine.ResolveEffectivePath(tempExe));
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [Fact]
    public void ResolveEffectivePath_returns_null_when_configured_is_blank_and_no_bundle()
    {
        // AppContext.BaseDirectory at unit-test time is the test bin
        // folder; it's intentionally NOT going to contain a
        // bbpPairings.exe (the test project doesn't bundle one).
        // So we expect null when no configured path is supplied.
        var bundledProbe = Path.Combine(System.AppContext.BaseDirectory, BbpPairingEngine.BundledExeName);
        if (File.Exists(bundledProbe))
        {
            // Defensive skip: if a developer happens to have copied
            // bbpPairings into the test bin folder we don't want a
            // false negative. Nothing meaningful to assert in that case.
            return;
        }

        Assert.Null(BbpPairingEngine.ResolveEffectivePath(null));
        Assert.Null(BbpPairingEngine.ResolveEffectivePath(""));
        Assert.Null(BbpPairingEngine.ResolveEffectivePath("   "));
    }

    [Fact]
    public void ResolveEffectivePath_returns_null_when_configured_path_does_not_exist()
    {
        var bundledProbe = Path.Combine(System.AppContext.BaseDirectory, BbpPairingEngine.BundledExeName);
        if (File.Exists(bundledProbe)) return;

        var bogus = Path.Combine(Path.GetTempPath(), $"definitely-not-here-{System.Guid.NewGuid():N}.exe");
        Assert.Null(BbpPairingEngine.ResolveEffectivePath(bogus));
    }

    [Fact]
    public void ResolveEffectivePath_falls_back_to_sibling_bundle_when_present()
    {
        // Drop a fake bbpPairings.exe next to AppContext.BaseDirectory
        // for the duration of this test so the resolver picks it up.
        var bundledProbe = Path.Combine(System.AppContext.BaseDirectory, BbpPairingEngine.BundledExeName);
        var preExisted = File.Exists(bundledProbe);
        if (!preExisted) File.WriteAllBytes(bundledProbe, [0]);
        try
        {
            // Configured path is bogus → resolver should fall back to bundle.
            var resolved = BbpPairingEngine.ResolveEffectivePath(
                Path.Combine(Path.GetTempPath(), $"missing-{System.Guid.NewGuid():N}.exe"));
            Assert.Equal(bundledProbe, resolved);

            // Same for null / empty / blank.
            Assert.Equal(bundledProbe, BbpPairingEngine.ResolveEffectivePath(null));
            Assert.Equal(bundledProbe, BbpPairingEngine.ResolveEffectivePath(""));
        }
        finally
        {
            if (!preExisted) File.Delete(bundledProbe);
        }
    }
}
