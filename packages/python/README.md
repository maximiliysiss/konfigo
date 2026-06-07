# Konfigo Python SDK

[![Python](https://img.shields.io/badge/Python-%3E%3D3.11-3776AB?logo=python&logoColor=white)](pyproject.toml)
[![PyPI](https://img.shields.io/badge/PyPI-konfigo-3775A9?logo=pypi&logoColor=white)](https://pypi.org/project/konfigo/)
[![Typed](https://img.shields.io/badge/typing-typed-blue)](pyproject.toml)
[![CI/CD](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml/badge.svg)](https://github.com/maximiliysiss/konfigo/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.0.1-blue)](../../VERSION)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Python client SDK for Konfigo Realtime Config. It mirrors the .NET SDK concepts:

- config groups and keys are declared with decorators;
- class/key schemas are discovered and sent to the backend as a version;
- config values are stored as string entries with per-key generations;
- stale and equal-generation updates are ignored;
- subscription events with generation `1` are treated as initial state and do not overwrite the already loaded snapshot.

## Install

```bash
pip install -e .
```

For the gRPC adapter, install the `grpc` extra — the `service_pb2` / `service_pb2_grpc` modules are pre-generated from `protos/service.proto` and ship with the package:

```bash
pip install -e ".[grpc]"
```

## Declaring Options

```python
from dataclasses import dataclass
from konfigo import config_group, config_key


@config_group(key="Payments", group_name="Payments", description="Payment gateway")
@dataclass
class PaymentsOptions:
    provider: str = config_key(default_value="Stripe")
    timeout_seconds: int = config_key(default_value="30")
```

## Runtime Usage

```python
from konfigo import KonfigoClient, RealtimeConfigOptions, bind_config, discover_definitions
from konfigo.grpc_transport import GrpcRealtimeConfigTransport

options = RealtimeConfigOptions(
    is_enabled=True,
    service_id="orders",
    version="1.0.0",
    url="realtime-config.internal:443",
)

transport = GrpcRealtimeConfigTransport(options.url)
definitions = discover_definitions(PaymentsOptions)
client = await KonfigoClient.create(
    options=options,
    transport=transport,
    definitions=definitions,
)

task = client.start_background_task()

payments = bind_config(client.store.snapshot(), PaymentsOptions)
```

Framework integrations can subscribe to `client.store.subscribe(...)` and rebind dataclasses when values change.
