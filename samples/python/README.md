# Konfigo Python Sample

Minimal async HTTP application that declares a typed Konfigo configuration
group, registers it in the local backend, watches live updates over gRPC, and
exposes the current values at `/options`.

## Run

Start the backend first:

```bash
cd ../../apps/backend
docker compose up -d
dotnet run --project src/Konfigo
```

Install the local SDK with gRPC extras and generate the Python protobuf modules:

```bash
cd packages/python
pip install -e ".[grpc]"
python -m grpc_tools.protoc \
  -I protos \
  --python_out=src/konfigo \
  --grpc_python_out=src/konfigo \
  protos/service.proto
```

Run the sample:

```bash
PYTHONPATH=../../packages/python/src python app.py
```

Open `http://127.0.0.1:8089/options`.

Environment variables:

| Variable | Default |
|----------|---------|
| `KONFIGO_GRPC_URL` | `localhost:8081` |
| `KONFIGO_SERVICE_ID` | `f89f7a09-d71d-459d-b02c-07213ed0eaa4` |
| `KONFIGO_VERSION` | `1.0.13` |
| `HTTP_ADDR` | `127.0.0.1:8089` |
