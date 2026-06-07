from __future__ import annotations

from collections.abc import AsyncIterator
from datetime import datetime, timezone
from typing import Any

from konfigo.models import (
    ConfigEntry,
    CreateVersionRequest,
    CreateVersionResponse,
    IsVersionExistRequest,
    IsVersionExistResponse,
    StartSubscribeRequest,
    SubscriptionEvent,
    ValueType,
)


class GrpcRealtimeConfigTransport:
    """gRPC transport adapter.

    This adapter expects Python modules generated from ``protos/service.proto``:
    ``konfigo.service_pb2`` and ``konfigo.service_pb2_grpc``. They are imported
    lazily so the core package can be tested without grpc/protobuf installed.
    """

    def __init__(self, url: str, *, insecure: bool = False) -> None:
        try:
            import grpc
            from konfigo import service_pb2, service_pb2_grpc
        except ImportError as exc:
            raise RuntimeError(
                "Install konfigo[grpc] and generate service_pb2/service_pb2_grpc "
                "from python/protos/service.proto before using GrpcRealtimeConfigTransport"
            ) from exc

        self._pb = service_pb2
        if insecure:
            channel = grpc.aio.insecure_channel(url)
        else:
            channel = grpc.aio.secure_channel(url, grpc.ssl_channel_credentials())
        self._stub = service_pb2_grpc.RealtimeConfigGrpcServiceStub(channel)

    async def is_version_exists(self, request: IsVersionExistRequest) -> IsVersionExistResponse:
        response = await self._stub.IsVersionExists(
            self._pb.IsVersionExistRequest(service_id=request.service_id, version=request.version)
        )
        return IsVersionExistResponse(version_id=_unwrap_string(response.version_id))

    async def create_version(self, request: CreateVersionRequest) -> CreateVersionResponse:
        response = await self._stub.CreateVersion(_map_create_version_request(self._pb, request))
        return CreateVersionResponse(version_id=response.version_id)

    async def get_config(self, service_id: str, version: str) -> tuple[ConfigEntry, ...]:
        response = await self._stub.GetConfig(self._pb.GetConfigRequest(service_id=service_id, version=version))
        return tuple(_map_config_entry(item) for item in response.entries)

    async def start_subscribe(self, request: StartSubscribeRequest) -> AsyncIterator[SubscriptionEvent]:
        stream = self._stub.StartSubscribe(
            self._pb.StartSubscribeRequest(
                service_id=request.service_id,
                version_id=request.version_id,
                timestamp=_to_timestamp(self._pb, request.timestamp),
            )
        )
        async for event in stream:
            yield SubscriptionEvent(
                events=tuple(_map_config_entry(item) for item in event.events)
            )


def _map_create_version_request(pb: Any, request: CreateVersionRequest) -> Any:
    result = pb.CreateVersionRequest(service_id=request.service_id, version=request.version)
    for class_definition in request.classes:
        class_entry = result.classes.add(
            name=class_definition.name,
            description=_wrap_string(pb, class_definition.description),
        )
        for option in class_definition.options:
            class_entry.entries.add(
                key=option.key,
                name=option.name,
                description=_wrap_string(pb, option.description),
                value_type=option.type.value,
                enum_values=_wrap_string(pb, ",".join(option.enum_values or ())),
                value=_wrap_string(pb, option.default_value),
            )
    return result


def _map_config_entry(item: Any) -> ConfigEntry:
    return ConfigEntry(
        key=item.key,
        value=_unwrap_string(item.value),
        type=ValueType(item.type),
        generation=item.generation,
        timestamp=_from_timestamp(item.timestamp),
    )


def _wrap_string(pb: Any, value: str | None) -> Any:
    return None if value is None else pb.google_dot_protobuf_dot_wrappers__pb2.StringValue(value=value)


def _unwrap_string(value: Any) -> str | None:
    if value is None:
        return None
    return getattr(value, "value", None)


def _to_timestamp(pb: Any, value: datetime) -> Any:
    timestamp = pb.google_dot_protobuf_dot_timestamp__pb2.Timestamp()
    timestamp.FromDatetime(value if value.tzinfo else value.replace(tzinfo=timezone.utc))
    return timestamp


def _from_timestamp(value: Any) -> datetime:
    return value.ToDatetime().replace(tzinfo=timezone.utc)
