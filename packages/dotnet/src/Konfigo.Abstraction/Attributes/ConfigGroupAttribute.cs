using System;

namespace Konfigo.Abstraction.Attributes;

/// <summary>
/// Marks a class as a Realtime config group. The class is scanned on startup,
/// its <see cref="ConfigKeyAttribute"/>-annotated properties become config entries,
/// and <see cref="Key"/> is used as the configuration section name when binding via
/// <c>IOptions&lt;T&gt;</c>.
/// </summary>
/// <param name="key">
/// Section name used to bind the class to the <c>IConfiguration</c> provider.
/// When <c>null</c>, the class name is used.
/// </param>
/// <param name="groupName">Display name of the group shown in the UI. Falls back to the class name.</param>
/// <param name="description">Human-readable description of the group shown in the UI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigGroupAttribute(string? key = null, string? groupName = null, string? description = null) : Attribute
{
    /// <summary>
    /// Configuration section name used to bind this group.
    /// </summary>
    public string? Key { get; } = key;

    /// <summary>
    /// Display name of the group shown in the UI.
    /// </summary>
    public string? GroupName { get; } = groupName;

    /// <summary>
    /// Human-readable description of the group shown in the UI.
    /// </summary>
    public string? Description { get; } = description;
}
