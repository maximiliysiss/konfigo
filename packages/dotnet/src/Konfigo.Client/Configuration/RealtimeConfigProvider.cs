using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Konfigo.Client.Entities;
using Konfigo.Client.Extensions;
using Konfigo.Client.Infrastructure.Extensions;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.Configuration;

internal sealed class RealtimeConfigProvider : ConfigurationProvider
{
    private readonly Dictionary<string, int> _generations = [];

    private const string TimestampKey = $"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.Timestamp)}";
    private DateTimeOffset _timestamp = DateTimeOffset.MinValue;

    private const string VersionIdKey = $"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.VersionId)}";
    private readonly VersionId _versionId;

    private readonly ConfigEntry[] _entries;

    private readonly ILogger<RealtimeConfigProvider> _logger;

    public RealtimeConfigProvider(VersionId versionId, ConfigEntry[] entries, ILogger<RealtimeConfigProvider> logger)
    {
        _versionId = versionId;
        _entries = entries;
        _logger = logger;
    }

    public override void Load()
    {
        _logger.LoadingRealtimeConfig();

        Update(_entries);

        Data[VersionIdKey] = _versionId.Value;

        _logger.RealtimeConfigLoaded();
    }

    private bool Update(IEnumerable<ConfigEntry> values)
    {
        var isUpdated = false;

        foreach (var (key, value, type, newGeneration, ts) in values)
        {
            if (_generations.TryGetValue(key, out var current) && current >= newGeneration)
                continue;

            foreach (var (k, v) in Unwind(key, type, value))
                Data[k] = v;

            _generations[key] = newGeneration;

            _timestamp = DateTimeOffsets.Max(_timestamp, ts);

            isUpdated = true;
        }

        Data[TimestampKey] = _timestamp.ToString(CultureInfo.InvariantCulture);

        return isUpdated;

        IEnumerable<(string key, string? value)> Unwind(string key, ValueType type, string? value)
        {
            if (type is not ValueType.Json && type is not ValueType.Array || string.IsNullOrEmpty(value))
                return [(key, value)];

            return JsonDocument.Parse(value).AsKeyValuePairs(key).DefaultIfEmpty((key, string.Empty));
        }
    }

    public void Set(IEnumerable<ConfigEntry> values)
    {
        _logger.SettingRealtimeConfig();

        var isUpdate = Update(values);

        if (isUpdate)
        {
            OnReload();
        }

        _logger.RealtimeConfigSet();
    }
}
