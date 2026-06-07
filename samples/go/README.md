# Konfigo Go Sample

Minimal HTTP application that declares a typed Konfigo configuration group,
registers it in the local backend, watches live updates over gRPC, and exposes
the current values at `/options`.

## Run

Start the backend first:

```bash
cd ../../apps/backend
docker compose up -d
dotnet run --project src/Konfigo
```

Then run the sample:

```bash
cd samples/go
go run .
```

Open `http://localhost:8088/options`.

Environment variables:

| Variable | Default |
|----------|---------|
| `KONFIGO_GRPC_URL` | `localhost:8081` |
| `KONFIGO_SERVICE_ID` | `f89f7a09-d71d-459d-b02c-07213ed0eaa4` |
| `KONFIGO_VERSION` | `1.0.14` |
| `HTTP_ADDR` | `:8088` |
