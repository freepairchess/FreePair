using System.IO;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests for <see cref="TournamentLock"/> — the per-file mutex
/// FreePair uses to prevent two app instances from auto-saving
/// into the same .sjson at the same time.
/// </summary>
public class TournamentLockTests
{
    [Fact]
    public void TryAcquire_succeeds_then_blocks_a_second_attempt_on_same_path()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fp-lock-{System.Guid.NewGuid():N}.sjson");
        // No need for the file to exist — the mutex name is
        // path-derived, not contents-derived.
        using var first = TournamentLock.TryAcquire(path);
        Assert.NotNull(first);

        var second = TournamentLock.TryAcquire(path);
        Assert.Null(second);
    }

    [Fact]
    public void TryAcquire_succeeds_after_first_lease_disposed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fp-lock-{System.Guid.NewGuid():N}.sjson");
        var first = TournamentLock.TryAcquire(path);
        Assert.NotNull(first);
        first!.Dispose();

        using var second = TournamentLock.TryAcquire(path);
        Assert.NotNull(second);
    }

    [Fact]
    public void TryAcquire_two_distinct_paths_do_not_collide()
    {
        var pathA = Path.Combine(Path.GetTempPath(), $"fp-lock-{System.Guid.NewGuid():N}.sjson");
        var pathB = Path.Combine(Path.GetTempPath(), $"fp-lock-{System.Guid.NewGuid():N}.sjson");

        using var lockA = TournamentLock.TryAcquire(pathA);
        using var lockB = TournamentLock.TryAcquire(pathB);
        Assert.NotNull(lockA);
        Assert.NotNull(lockB);
    }

    [Fact]
    public void MakeMutexName_is_stable_for_the_same_canonical_path()
    {
        var path = Path.Combine(Path.GetTempPath(), "stable.sjson");
        Assert.Equal(
            TournamentLock.MakeMutexName(path),
            TournamentLock.MakeMutexName(path));
    }

    [Fact]
    public void MakeMutexName_differs_across_paths()
    {
        var a = TournamentLock.MakeMutexName(Path.Combine(Path.GetTempPath(), "a.sjson"));
        var b = TournamentLock.MakeMutexName(Path.Combine(Path.GetTempPath(), "b.sjson"));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MakeMutexName_treats_relative_and_absolute_paths_identically()
    {
        // Path.GetFullPath("foo.sjson") canonicalises against
        // Environment.CurrentDirectory, so the same name expressed
        // either way should hash to the same mutex.
        var cwd = System.IO.Directory.GetCurrentDirectory();
        var rel = "freepair-test.sjson";
        var abs = Path.Combine(cwd, rel);
        Assert.Equal(
            TournamentLock.MakeMutexName(rel),
            TournamentLock.MakeMutexName(abs));
    }
}
