# Konfigo Python SDK

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

For the gRPC adapter:

```bash
pip install -e ".[grpc]"
python -m grpc_tools.protoc \
  -I protos \
  --python_out=src/konfigo \
  --grpc_python_out=src/konfigo \
  protos/service.proto
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
