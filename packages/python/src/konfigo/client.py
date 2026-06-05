from __future__ import annotations

import asyncio
from contextlib import suppress
from dataclasses import dataclass
from datetime import datetime
import logging

from konfigo.definitions import ClassDefinition
from konfigo.models import (
    CreateVersionRequest,
    ConfigEntry,
    IsVersionExistRequest,
    RealtimeConfigOptions,
    StartSubscribeRequest,
    VersionId,
)
from konfigo.store import RealtimeConfigStore
from konfigo.transport import RealtimeConfigTransport

logger = logging.getLogger(__name__)


@dataclass(slots=True)
class KonfigoClient:
    options: RealtimeConfigOptions
    transport: RealtimeConfigTransport
    definitions: tuple[ClassDefinition, ...]
    store: RealtimeConfigStore

    async def ensure_version(self) -> VersionId:
        existing = await self.transport.is_version_exists(
            IsVersionExistRequest(service_id=self.options.service_id, version=self.options.version)
        )
        if existing.version_id is not None:
            return VersionId(existing.version_id)

        created = await self.transport.create_version(
            CreateVersionRequest(
                service_id=self.options.service_id,
                version=self.options.version,
                classes=self.definitions,
            )
        )
        return VersionId(created.version_id)

    async def watch_forever(self) -> None:
        if not self.options.is_enabled:
            await asyncio.Event().wait()

        if not self.definitions:
            logger.warning("No Konfigo config definitions were provided")
            await asyncio.Event().wait()

        version_id = await self.ensure_version()
        timestamp = self.options.timestamp

        while True:
            try:
                request = StartSubscribeRequest(
                    service_id=self.options.service_id,
                    version_id=version_id.value,
                    timestamp=timestamp,
                )

                async for event in self.transport.start_subscribe(request):
                    updates = tuple(entry for entry in event.events if entry.generation > 1)
                    if not updates:
                        continue

                    self.store.update(updates)
                    timestamp = _max_timestamp(timestamp, updates)
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Konfigo subscription cycle failed")
                await asyncio.sleep(self.options.polling_interval.total_seconds())

    def start_background_task(self) -> asyncio.Task[None]:
        return asyncio.create_task(self.watch_forever())

    @classmethod
    async def create(
        cls,
        *,
        options: RealtimeConfigOptions,
        transport: RealtimeConfigTransport,
        definitions: tuple[ClassDefinition, ...],
        initial_entries: tuple[ConfigEntry, ...] = (),
    ) -> "KonfigoClient":
        store = RealtimeConfigStore(initial_entries)
        return cls(options=options, transport=transport, definitions=definitions, store=store)

    async def close_task(self, task: asyncio.Task[None]) -> None:
        task.cancel()
        with suppress(asyncio.CancelledError):
            await task


def _max_timestamp(current: datetime, entries: tuple[ConfigEntry, ...]) -> datetime:
    for entry in entries:
        current = max(current, entry.timestamp)
    return current
