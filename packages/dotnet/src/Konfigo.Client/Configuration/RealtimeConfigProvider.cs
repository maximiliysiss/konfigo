using System;
using System.Collections.Generic;
using System.Globalization;
using Konfigo.Client.Extensions;
using Konfigo.Client.Infrastructure.Extensions;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Konfigo.Client.Configuration;

internal sealed class RealtimeConfigProvider(ConfigEntry[] entries, ILogger<RealtimeConfigProvider> logger) : ConfigurationProvider
{
    private readonly Dictionary<string, int> _generations = [];

    private const string TimestampKey = $"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.Timestamp)}";
    private DateTimeOffset _timestamp = DateTimeOffset.MinValue;

    public override void Load()
    {
        logger.LoadingRealtimeConfig();

        Update(entries);

        logger.RealtimeConfigLoaded();
    }

    private bool Update(IEnumerable<ConfigEntry> values)
    {
        var isUpdated = false;

        foreach (var (key, value, newGeneration, ts) in values)
        {
            if (_generations.TryGetValue(key, out var current) && current >= newGeneration)
            {
                continue;
            }

            Data[key] = value;
            _generations[key] = newGeneration;

            _timestamp = DateTimeOffsets.Max(_timestamp, ts);

            isUpdated = true;
        }

        Data[TimestampKey] = _timestamp.ToString(CultureInfo.InvariantCulture);

        return isUpdated;
    }

    public void Set(IEnumerable<ConfigEntry> values)
    {
        logger.SettingRealtimeConfig();

        var isUpdate = Update(values);

        if (isUpdate)
        {
            OnReload();
        }

        logger.RealtimeConfigSet();
    }
}
