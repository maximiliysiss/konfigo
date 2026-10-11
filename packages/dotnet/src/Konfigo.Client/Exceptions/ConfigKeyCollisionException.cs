using System;
using System.Collections.Generic;

namespace Konfigo.Client.Exceptions;

/// <summary>
/// Thrown when the same config key is declared by several config groups with different value types.
/// </summary>
public sealed class ConfigKeyCollisionException : InvalidOperationException
{
    internal ConfigKeyCollisionException(IReadOnlyCollection<string> collisions)
        : base($"Konfigo config key collisions detected:{Environment.NewLine}{string.Join(Environment.NewLine, collisions)}")
    {
        Collisions = collisions;
    }

    /// <summary>
    /// Human-readable description of every detected collision.
    /// </summary>
    public IReadOnlyCollection<string> Collisions { get; }
}
