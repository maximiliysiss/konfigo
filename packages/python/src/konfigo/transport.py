from __future__ import annotations

from collections.abc import AsyncIterator
from typing import Protocol

from konfigo.models import (
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
