from __future__ import annotations

from dataclasses import Field, dataclass, field, fields, is_dataclass
from datetime import date, datetime, timedelta
from enum import Enum
import inspect
from types import NoneType, UnionType
from typing import Any, Iterable, Mapping, Sequence, Union, get_args, get_origin, get_type_hints

from konfigo.models import ValueType


KONFIGO_GROUP_ATTR = "__konfigo_group__"
KONFIGO_KEY_METADATA = "konfigo_key"


@dataclass(frozen=True, slots=True)
class ConfigKeyMetadata:
    name: str | None = None
    description: str | None = None
    default_value: str | None = None


@dataclass(frozen=True, slots=True)
class OptionDefinition:
    key: str
    name: str
    description: str | None
    type: ValueType
    default_value: str | None
    enum_values: tuple[str, ...] | None = None


@dataclass(frozen=True, slots=True)
class ClassDefinition:
    key: str
    name: str
    description: str | None
    options: tuple[OptionDefinition, ...]
    type: type[Any]


@dataclass(frozen=True, slots=True)
class _ConfigGroupMetadata:
    key: str | None = None
    group_name: str | None = None
    description: str | None = None


def config_group(
    cls: type[Any] | None = None,
    *,
    key: str | None = None,
    group_name: str | None = None,
    description: str | None = None,
):
    def decorate(target: type[Any]) -> type[Any]:
        setattr(target, KONFIGO_GROUP_ATTR, _ConfigGroupMetadata(key, group_name, description))
        return target

    if cls is None:
        return decorate

    return decorate(cls)


def config_key(
    *,
    name: str | None = None,
    description: str | None = None,
    default_value: str | None = None,
    default: Any = None,
    default_factory: Any = None,
):
    metadata = {KONFIGO_KEY_METADATA: ConfigKeyMetadata(name, description, default_value)}
    kwargs: dict[str, Any] = {"metadata": metadata}

    if default_factory is not None:
        kwargs["default_factory"] = default_factory
    else:
        kwargs["default"] = default

    return field(**kwargs)


def discover_definitions(*modules_or_classes: Any) -> tuple[ClassDefinition, ...]:
    definitions: list[ClassDefinition] = []

    for item in modules_or_classes:
        candidates = [item] if inspect.isclass(item) else _classes_from_module(item)
        for cls in candidates:
            definition = definition_from_class(cls)
            if definition is not None and definition.options:
                definitions.append(definition)

    return tuple(definitions)


def definition_from_class(cls: type[Any]) -> ClassDefinition | None:
    group = getattr(cls, KONFIGO_GROUP_ATTR, None)
    if group is None:
        return None
    if not is_dataclass(cls):
        raise TypeError(f"{cls.__qualname__} must be a dataclass to be used as a Konfigo config group")

    section_key = group.key or cls.__name__
    type_hints = get_type_hints(cls)
    options = tuple(
        _option_from_field(section_key, item, type_hints.get(item.name, item.type))
        for item in fields(cls)
        if KONFIGO_KEY_METADATA in item.metadata
    )

    return ClassDefinition(
        key=section_key,
        name=group.group_name or cls.__name__,
        description=group.description,
        options=options,
        type=cls,
    )


def _option_from_field(section_key: str, item: Field[Any], annotation: Any) -> OptionDefinition:
    metadata: ConfigKeyMetadata = item.metadata[KONFIGO_KEY_METADATA]
    field_annotation = annotation
    annotation = _strip_optional(field_annotation)
    value_type = _map_type(annotation)
    enum_values = tuple(member.name for member in annotation) if _is_enum(annotation) else None
    default_value = metadata.default_value

    if default_value is None and not _is_optional(field_annotation):
        default_value = _map_default_value(value_type, annotation)

    return OptionDefinition(
        key=f"{section_key}:{item.name}",
        name=metadata.name or item.name,
        description=metadata.description,
        type=value_type,
        default_value=default_value,
        enum_values=enum_values,
    )


def _classes_from_module(module: Any) -> Iterable[type[Any]]:
    for _, value in inspect.getmembers(module, inspect.isclass):
        if value.__module__ == getattr(module, "__name__", None):
            yield value


def _strip_optional(annotation: Any) -> Any:
    if _is_optional(annotation):
        return next(arg for arg in get_args(annotation) if arg is not NoneType)
    return annotation


def _is_optional(annotation: Any) -> bool:
    origin = get_origin(annotation)
    if origin not in (Union, UnionType):
        return False
    return NoneType in get_args(annotation)


def _map_type(annotation: Any) -> ValueType:
    origin = get_origin(annotation) or annotation
    if annotation is str:
        return ValueType.STRING
    if annotation is bool:
        return ValueType.BOOLEAN
    if annotation in (datetime, date):
        return ValueType.DATE_TIME
    if annotation is timedelta:
        return ValueType.TIME_SPAN
    if _is_enum(annotation):
        return ValueType.ENUM
    if annotation in (int, float, complex):
        return ValueType.NUMBER
    if origin in (list, tuple, set, Sequence):
        return ValueType.ARRAY
    if origin in (dict, Mapping):
        return ValueType.JSON
    return ValueType.JSON


def _map_default_value(value_type: ValueType, annotation: Any) -> str | None:
    if value_type is ValueType.ARRAY:
        return "[]"
    if value_type is ValueType.STRING:
        return ""
    if value_type is ValueType.DATE_TIME:
        return "0001-01-01T00:00:00+00:00"
    if value_type is ValueType.TIME_SPAN:
        return "00:00:00"
    if value_type is ValueType.JSON:
        return "{}"
    if value_type is ValueType.ENUM:
        return next(iter(annotation)).name
    if value_type is ValueType.NUMBER:
        return "0"
    if value_type is ValueType.BOOLEAN:
        return "false"
    return None


def _is_enum(annotation: Any) -> bool:
    return inspect.isclass(annotation) and issubclass(annotation, Enum)
