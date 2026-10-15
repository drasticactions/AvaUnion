using System;

namespace AvaUnion.Engine.Native;

/// <summary>The exception that occurs when an app asks for the native engine and the engine cannot start.</summary>
public sealed class NativeInitException : Exception
{
    /// <summary>Creates the exception.</summary>
    public NativeInitException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception.</summary>
    public NativeInitException()
    {
    }

    /// <summary>Creates the exception.</summary>
    public NativeInitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
