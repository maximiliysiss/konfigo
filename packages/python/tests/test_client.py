from __future__ import annotations

import asyncio
from collections.abc import AsyncIterator
from dataclasses import dataclass
from datetime import datetime, timezone

from konfigo import (
    ConfigEntry,
    CreateVersionRequest,
    CreateVersionResponse,
    IsVersionExistRequest,
    IsVersionExistResponse,
    KonfigoClient,
    RealtimeConfigOptions,
    StartSubscribeRequest,
    SubscriptionEvent,
    config_group,
    config_key,
    discover_definitions,
)


@config_group
@dataclass
class ChangedOptions:
    value: int = config_key(default_value="42")


class FakeTransport:
    def __init__(self) -> None:
        self.created: CreateVersionRequest | None = None
        self.version_id: str | None = None
        self.events: asyncio.Queue[SubscriptionEvent] = asyncio.Queue()

    async def is_version_exists(self, request: IsVersionExistRequest) -> IsVersionExistResponse:
        return IsVersionExistResponse(version_id=self.version_id)

    async def create_version(self, request: CreateVersionRequest) -> CreateVersionResponse:
        self.created = request
        return CreateVersionResponse(version_id="version-1")

    async def start_subscribe(self, request: StartSubscribeRequest) -> AsyncIterator[SubscriptionEvent]:
        while True:
            yield await self.events.get()


async def test_ensure_version_creates_when_missing():
    transport = FakeTransport()
    client = await KonfigoClient.create(
        options=RealtimeConfigOptions(is_enabled=True, service_id="orders", version="1.0.0"),
        transport=transport,
        definitions=discover_definitions(ChangedOptions),
    )

    version = await client.ensure_version()

    assert version.value == "version-1"
    assert transport.created is not None
    assert transport.created.service_id == "orders"


async def test_watch_forever_filters_initial_generation_and_applies_updates():
    transport = FakeTransport()
    client = await KonfigoClient.create(
        options=RealtimeConfigOptions(is_enabled=True, service_id="orders", version="1.0.0"),
        transport=transport,
        definitions=discover_definitions(ChangedOptions),
    )
    task = client.start_background_task()

    await transport.events.put(
        SubscriptionEvent(
            events=(
                ConfigEntry("ChangedOptions:value", "43", 1, datetime.now(timezone.utc)),
                ConfigEntry("ChangedOptions:value", "44", 2, datetime.now(timezone.utc)),
            )
        )
    )

    for _ in range(20):
        if client.store.get("ChangedOptions:value") == "44":
            break
        await asyncio.sleep(0.01)

    await client.close_task(task)

    assert client.store.get("ChangedOptions:value") == "44"
