using System;

namespace Konfigo.Abstraction.Attributes;

/// <summary>
/// Marks a property inside a <see cref="ConfigGroupAttribute"/>-annotated class
/// as a Realtime config entry. The property is registered as a config key under
/// its owning group and its value is sourced from the Realtime configuration
/// provider at runtime.
/// </summary>
/// <param name="name">Display name shown in the UI. Falls back to the property name.</param>
/// <param name="description">Human-readable description shown in the UI.</param>
/// <param name="defaultValue">
/// String-encoded default value used when the remote service has no value yet.
/// When <c>null</c>, a type-driven default is inferred (e.g. <c>"0"</c> for numbers,
/// <c>"false"</c> for booleans, <c>"[]"</c> for arrays).
/// </param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ConfigKeyAttribute(string? name = null, string? description = null, string? defaultValue = null) : Attribute
{
    /// <summary>
    /// Display name of the key shown in the UI.
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// Human-readable description of the key shown in the UI.
    /// </summary>
    public string? Description { get; set; } = description;

    /// <summary>
    /// String-encoded default value of the key.
    /// </summary>
    public string? DefaultValue { get; set; } = defaultValue;
}
