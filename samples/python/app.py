from __future__ import annotations

import asyncio
from dataclasses import asdict, dataclass
from datetime import timedelta
import json
import os
from typing import Any
from urllib.parse import urlparse

from konfigo import (
    KonfigoClient,
    RealtimeConfigOptions,
    bind_config,
    config_group,
    config_key,
    discover_definitions,
)
from konfigo.grpc_transport import GrpcRealtimeConfigTransport


@config_group(key="Payments", group_name="Payments", description="Payment gateway")
@dataclass
class PaymentsOptions:
    provider: str = config_key(
        name="Provider",
        description="Payment provider",
        default_value="Stripe",
    )
    timeout: timedelta = config_key(
        name="Timeout",
        description="Provider request timeout",
        default_value="00:00:30",
    )
    retry_count: int = config_key(
        name="Retry count",
        description="Retry attempts before failing",
        default_value="3",
    )
    enable_fallback: bool = config_key(
        name="Enable fallback",
        description="Use fallback provider",
        default_value="true",
    )


async def main() -> None:
    grpc_url = os.getenv("KONFIGO_GRPC_URL", "localhost:8081")
    service_id = os.getenv("KONFIGO_SERVICE_ID", "f89f7a09-d71d-459d-b02c-07213ed0eaa4")
    version = os.getenv("KONFIGO_VERSION", "1.0.13")
    http_addr = os.getenv("HTTP_ADDR", "127.0.0.1:8089")

    options = RealtimeConfigOptions(
        is_enabled=True,
        service_id=service_id,
        version=version,
        url=grpc_url,
    )
    transport = GrpcRealtimeConfigTransport(options.url, insecure=True)
    definitions = discover_definitions(PaymentsOptions)

    client = await KonfigoClient.create(
        options=options,
        transport=transport,
        definitions=definitions,
    )
    await client.ensure_version()
    initial_entries = await transport.get_config(service_id, version)
    client.store.update(initial_entries, notify=False)

    task = client.start_background_task()
    try:
        server = await asyncio.start_server(
            lambda reader, writer: handle_request(reader, writer, client),
            host=http_addr_host(http_addr),
            port=http_addr_port(http_addr),
        )

        async with server:
            print(f"Python sample listening on http://{http_addr}/options")
            await server.serve_forever()
    finally:
        await client.close_task(task)


async def handle_request(
    reader: asyncio.StreamReader,
    writer: asyncio.StreamWriter,
    client: KonfigoClient,
) -> None:
    request_line = await reader.readline()
    method, path, _ = request_line.decode("ascii", errors="ignore").split(" ", 2)

    while await reader.readline() not in (b"\r\n", b"\n", b""):
        pass

    if method != "GET" or path != "/options":
        await write_response(writer, 404, {"error": "not found"})
        return

    payments = bind_config(client.store.snapshot(), PaymentsOptions)
    await write_response(writer, 200, asdict(payments), default=str)


async def write_response(
    writer: asyncio.StreamWriter,
    status: int,
    body: dict[str, Any],
    *,
    default: Any = None,
) -> None:
    reason = "OK" if status == 200 else "Not Found"
    payload = json.dumps(body, default=default).encode("utf-8")
    headers = (
        f"HTTP/1.1 {status} {reason}\r\n"
        "Content-Type: application/json\r\n"
        f"Content-Length: {len(payload)}\r\n"
        "Connection: close\r\n"
        "\r\n"
    ).encode("ascii")

    writer.write(headers + payload)
    await writer.drain()
    writer.close()
    await writer.wait_closed()


def http_addr_host(value: str) -> str:
    parsed = parse_http_addr(value)
    return parsed.hostname or "127.0.0.1"


def http_addr_port(value: str) -> int:
    parsed = parse_http_addr(value)
    if parsed.port is None:
        raise ValueError(f"HTTP_ADDR must include a port: {value}")
    return parsed.port


def parse_http_addr(value: str):
    return urlparse(f"//{value}")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
