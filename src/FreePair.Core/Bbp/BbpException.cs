using System;

namespace FreePair.Core.Bbp;

/// <summary>Base class for all BBP-related failures.</summary>
public class BbpException : Exception
{
    public BbpException(string message) : base(message) { }
    public BbpException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when the pairing engine is not installed or its path in settings
/// does not point at an existing file. The message is intended to be shown
/// directly to the user.
/// </summary>
public class BbpNotConfiguredException : BbpException
{
    public BbpNotConfiguredException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the BBP process exits non-zero or writes diagnostic output
/// we cannot recover from.
/// </summary>
public class BbpExecutionException : BbpException
{
    public int ExitCode { get; }

    public BbpExecutionException(int exitCode, string message) : base(message)
    {
        ExitCode = exitCode;
    }
}
