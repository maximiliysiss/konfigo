from __future__ import annotations

from dataclasses import fields, is_dataclass
from datetime import datetime, timedelta
from enum import Enum
import json
from types import NoneType, UnionType
from typing import Any, Union, get_args, get_origin, get_type_hints

from konfigo.definitions import KONFIGO_GROUP_ATTR, KONFIGO_KEY_METADATA


def bind_config(values: dict[str, str | None], cls: type[Any]) -> Any:
    if not is_dataclass(cls):
        raise TypeError(f"{cls.__qualname__} must be a dataclass")

    group = getattr(cls, KONFIGO_GROUP_ATTR, None)
    if group is None:
        raise TypeError(f"{cls.__qualname__} is not marked with @config_group")

    section_key = group.key or cls.__name__
    kwargs: dict[str, Any] = {}
    type_hints = get_type_hints(cls)

    for item in fields(cls):
        if KONFIGO_KEY_METADATA not in item.metadata:
            continue

        raw = values.get(f"{section_key}:{item.name}")
        if raw is None:
            continue

        kwargs[item.name] = parse_value(raw, type_hints.get(item.name, item.type))

    return cls(**kwargs)


def parse_value(raw: str, annotation: Any) -> Any:
    annotation = _strip_optional(annotation)
    origin = get_origin(annotation) or annotation

    if annotation is str:
        return raw
    if annotation is bool:
        return raw.strip().lower() in {"1", "true", "yes", "on"}
    if annotation is int:
        return int(raw)
    if annotation is float:
        return float(raw)
    if annotation is complex:
        return complex(raw)
    if annotation is datetime:
        return datetime.fromisoformat(raw.replace("Z", "+00:00"))
    if annotation is timedelta:
        return _parse_timedelta(raw)
    if isinstance(annotation, type) and issubclass(annotation, Enum):
        return annotation[raw]
    if origin in (list, tuple, set, dict):
        value = json.loads(raw)
        if origin is tuple:
            return tuple(value)
        if origin is set:
            return set(value)
        return value

    return json.loads(raw)


def _parse_timedelta(raw: str) -> timedelta:
    parts = raw.split(":")
    if len(parts) != 3:
        raise ValueError(f"Expected HH:MM:SS timedelta, got {raw!r}")

    hours, minutes, seconds = parts
    return timedelta(hours=int(hours), minutes=int(minutes), seconds=float(seconds))


def _strip_optional(annotation: Any) -> Any:
    origin = get_origin(annotation)
    if origin in (Union, UnionType) and NoneType in get_args(annotation):
        return next(arg for arg in get_args(annotation) if arg is not NoneType)
    return annotation
