from datetime import datetime, timezone

from konfigo import ConfigEntry, RealtimeConfigStore
from konfigo.store import TIMESTAMP_KEY


def entry(key: str = "Options:Value", value: str = "42", generation: int = 2) -> ConfigEntry:
    return ConfigEntry(key=key, value=value, generation=generation, timestamp=datetime.now(timezone.utc))


def test_update_sets_initial_config():
    store = RealtimeConfigStore([entry()])

    assert store.get("Options:Value") == "42"


def test_update_notifies_when_config_changed():
    store = RealtimeConfigStore()
    calls = []
    store.subscribe(lambda snapshot: calls.append(snapshot))

    assert store.update([entry()])

    assert calls[-1]["Options:Value"] == "42"


def test_update_does_not_notify_for_stale_generation():
    store = RealtimeConfigStore([entry(generation=5)])
    calls = []
    store.subscribe(lambda snapshot: calls.append(snapshot))

    assert not store.update([entry(value="44", generation=4)])

    assert calls == []
    assert store.get("Options:Value") == "42"


def test_update_does_not_override_equal_generation():
    store = RealtimeConfigStore([entry(generation=5)])

    assert not store.update([entry(value="44", generation=5)])

    assert store.get("Options:Value") == "42"


def test_update_overrides_greater_generation_and_timestamp():
    ts = datetime(2026, 1, 1, tzinfo=timezone.utc)
    store = RealtimeConfigStore([ConfigEntry("Options:Value", "42", 2, ts)])

    next_ts = datetime(2026, 1, 2, tzinfo=timezone.utc)
    assert store.update([ConfigEntry("Options:Value", "44", 3, next_ts)])

    assert store.get("Options:Value") == "44"
    assert store.get(TIMESTAMP_KEY) == next_ts.isoformat()
