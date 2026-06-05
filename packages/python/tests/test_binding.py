from dataclasses import dataclass
from datetime import datetime, timezone

from konfigo import ConfigEntry, RealtimeConfigStore, bind_config, config_group, config_key


@config_group
@dataclass
class SimpleOptions:
    value: int = config_key(default=42)
    enabled: bool = config_key(default=False)


def test_bind_config_returns_typed_dataclass():
    store = RealtimeConfigStore(
        [
            ConfigEntry("SimpleOptions:value", "44", 2, datetime.now(timezone.utc)),
            ConfigEntry("SimpleOptions:enabled", "true", 2, datetime.now(timezone.utc)),
        ]
    )

    options = bind_config(store.snapshot(), SimpleOptions)

    assert options.value == 44
    assert options.enabled is True
