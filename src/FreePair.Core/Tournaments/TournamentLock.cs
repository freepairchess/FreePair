using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Per-file exclusive lock used to prevent the same SwissSys
/// <c>.sjson</c> from being opened by two concurrent FreePair
/// instances at once. Both instances would otherwise auto-save on
/// every mutation and stomp each other's edits.
/// </summary>
/// <remarks>
/// <para>Backed by a named OS <see cref="System.Threading.Mutex"/>
/// keyed off a SHA-256 hash of the canonicalised absolute path
/// (lower-cased on Windows because the filesystem is
/// case-insensitive there). Mutex names have a length limit so we
/// truncate the hash to a comfortable 32-hex-char prefix.</para>
/// <para>The lock is non-blocking: <see cref="TryAcquire"/> returns
/// <c>null</c> immediately when another instance already owns it
/// — callers surface that as a user-facing error.</para>
/// </remarks>
public sealed class TournamentLock : IDisposable
{
    /// <summary>
    /// Process-local set of mutex names currently held by this
    /// instance. Needed because <see cref="System.Threading.Mutex"/>
    /// is re-entrant on the same thread — a second
    /// <see cref="TryAcquire"/> call on the same path within the
    /// same process would otherwise succeed and defeat the lock.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> s_localOwned =
        new(System.StringComparer.Ordinal);
    private static readonly object s_localOwnedGate = new();

    private readonly Mutex _mutex;
    private readonly string _mutexName;
    private bool _disposed;

    private TournamentLock(Mutex mutex, string mutexName)
    {
        _mutex = mutex;
        _mutexName = mutexName;
    }

    /// <summary>
    /// Attempts to take the lock for <paramref name="filePath"/>.
    /// Returns the disposable lease on success, <c>null</c> when
    /// another holder already exists (in this process or any other).
    /// </summary>
    public static TournamentLock? TryAcquire(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var name = MakeMutexName(filePath);

        // Block re-entry from the same process first, before we
        // touch the OS mutex. Mutex is thread-reentrant so the
        // OS-level WaitOne would otherwise return true for a
        // second call from the same thread.
        lock (s_localOwnedGate)
        {
            if (s_localOwned.Contains(name)) return null;
        }

        var mutex = new Mutex(initiallyOwned: false, name);
        bool acquired;
        try
        {
            acquired = mutex.WaitOne(System.TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // Previous owner crashed without releasing — we now own it.
            acquired = true;
        }

        if (!acquired)
        {
            mutex.Dispose();
            return null;
        }

        lock (s_localOwnedGate)
        {
            s_localOwned.Add(name);
        }
        return new TournamentLock(mutex, name);
    }

    /// <summary>
    /// Computes the OS-level mutex name for
    /// <paramref name="filePath"/>. Public so tests can assert
    /// stability + that distinct paths produce distinct names.
    /// </summary>
    public static string MakeMutexName(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var canonical = Path.GetFullPath(filePath);
        if (OperatingSystem.IsWindows())
        {
            canonical = canonical.ToLowerInvariant();
        }
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "FreePair_Tournament_" + Convert.ToHexString(hash)[..32];
    }

    /// <summary>
    /// Checks whether <paramref name="filePath"/> is currently
    /// owned by some <i>other</i> process. Returns <c>false</c> if
    /// the file is unlocked, owned by this process, or the probe
    /// itself failed (we err on the side of letting the caller
    /// proceed). The probe takes the lock momentarily and releases
    /// it immediately — there's a tiny race window where another
    /// process could grab it between the probe and a follow-up
    /// acquire, but that's fine because the follow-up acquire
    /// itself fails the same way and surfaces the same error.
    /// </summary>
    public static bool IsHeldByAnotherProcess(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var name = MakeMutexName(filePath);

        // If WE hold it, that's not "another process".
        lock (s_localOwnedGate)
        {
            if (s_localOwned.Contains(name)) return false;
        }

        // Check if a named mutex with that name already exists in
        // the kernel. If TryOpenExisting fails, no one in any
        // process has touched it ⇒ definitely not held.
        if (!Mutex.TryOpenExisting(name, out var existing))
        {
            return false;
        }
        try
        {
            return !existing.WaitOne(System.TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // Previous owner crashed — about to be re-acquirable.
            existing.ReleaseMutex();
            return false;
        }
        finally
        {
            try { existing.ReleaseMutex(); } catch { /* not held by us */ }
            existing.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        try { _mutex.ReleaseMutex(); } catch (System.ApplicationException) { /* not owned */ }
        _mutex.Dispose();
        lock (s_localOwnedGate)
        {
            s_localOwned.Remove(_mutexName);
        }
        _disposed = true;
    }
}
