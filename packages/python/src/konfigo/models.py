from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone, timedelta
from enum import Enum
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from konfigo.definitions import ClassDefinition


class ValueType(Enum):
    UNKNOWN = 0
    STRING = 1
    BOOLEAN = 2
    DATE_TIME = 3
    TIME_SPAN = 4
    ENUM = 5
    NUMBER = 6
    ARRAY = 7
    JSON = 8


@dataclass(frozen=True, slots=True)
class ConfigEntry:
    key: str
    value: str | None
    generation: int
    timestamp: datetime
    type: ValueType = ValueType.UNKNOWN


@dataclass(frozen=True, slots=True)
class VersionId:
    value: str


@dataclass(slots=True)
class RealtimeConfigOptions:
    is_enabled: bool = False
    service_id: str = ""
    version: str = ""
    url: str = ""
    timestamp: datetime = field(default_factory=lambda: datetime.min.replace(tzinfo=timezone.utc))
    polling_interval: timedelta = field(default_factory=lambda: timedelta(seconds=5))
    initial_request_delay: timedelta = field(default_factory=lambda: timedelta(seconds=10))


@dataclass(frozen=True, slots=True)
class IsVersionExistRequest:
    service_id: str
    version: str


@dataclass(frozen=True, slots=True)
class IsVersionExistResponse:
    version_id: str | None


@dataclass(frozen=True, slots=True)
class CreateVersionRequest:
    service_id: str
    version: str
    classes: tuple["ClassDefinition", ...]


@dataclass(frozen=True, slots=True)
class CreateVersionResponse:
    version_id: str


@dataclass(frozen=True, slots=True)
class StartSubscribeRequest:
    service_id: str
    version_id: str
    timestamp: datetime


@dataclass(frozen=True, slots=True)
class SubscriptionEvent:
    events: tuple[ConfigEntry, ...]
