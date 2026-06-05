from __future__ import annotations

from collections.abc import Callable, Iterable, Mapping
from datetime import datetime, timezone
import threading

from konfigo.models import ConfigEntry

TIMESTAMP_KEY = "RealtimeConfigOptions:Timestamp"


class RealtimeConfigStore:
    def __init__(self, entries: Iterable[ConfigEntry] = ()) -> None:
        self._data: dict[str, str | None] = {}
        self._generations: dict[str, int] = {}
        self._timestamp = datetime.min.replace(tzinfo=timezone.utc)
        self._callbacks: list[Callable[[Mapping[str, str | None]], None]] = []
        self._lock = threading.RLock()
        self.update(entries, notify=False)

    @property
    def timestamp(self) -> datetime:
        with self._lock:
            return self._timestamp

    def snapshot(self) -> dict[str, str | None]:
        with self._lock:
            return dict(self._data)

    def get(self, key: str, default: str | None = None) -> str | None:
        with self._lock:
            return self._data.get(key, default)

    def subscribe(self, callback: Callable[[Mapping[str, str | None]], None]) -> Callable[[], None]:
        with self._lock:
            self._callbacks.append(callback)

        def unsubscribe() -> None:
            with self._lock:
                if callback in self._callbacks:
                    self._callbacks.remove(callback)

        return unsubscribe

    def update(self, entries: Iterable[ConfigEntry], *, notify: bool = True) -> bool:
        with self._lock:
            updated = False
            for entry in entries:
                current_generation = self._generations.get(entry.key)
                if current_generation is not None and current_generation >= entry.generation:
                    continue

                self._data[entry.key] = entry.value
                self._generations[entry.key] = entry.generation
                self._timestamp = max(self._timestamp, _normalized(entry.timestamp))
                updated = True

            self._data[TIMESTAMP_KEY] = self._timestamp.isoformat()
            callbacks = tuple(self._callbacks) if updated and notify else ()
            snapshot = dict(self._data) if callbacks else {}

        for callback in callbacks:
            callback(snapshot)

        return updated


def _normalized(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)
