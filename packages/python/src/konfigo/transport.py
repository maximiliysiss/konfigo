from __future__ import annotations

from collections.abc import AsyncIterator
from typing import Protocol

from konfigo.models import (
    ConfigEntry,
    CreateVersionRequest,
    CreateVersionResponse,
    IsVersionExistRequest,
    IsVersionExistResponse,
    StartSubscribeRequest,
    SubscriptionEvent,
)


class RealtimeConfigTransport(Protocol):
    async def is_version_exists(self, request: IsVersionExistRequest) -> IsVersionExistResponse:
        ...

    async def create_version(self, request: CreateVersionRequest) -> CreateVersionResponse:
        ...

    def start_subscribe(self, request: StartSubscribeRequest) -> AsyncIterator[SubscriptionEvent]:
        ...


class InitialConfigTransport(RealtimeConfigTransport, Protocol):
    async def get_config(self, service_id: str, version: str) -> tuple[ConfigEntry, ...]:
        ...
