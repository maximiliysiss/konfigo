# Python SDK

The Python SDK (`konfigo`, distributed via PyPI) discovers configuration schemas from
`@config_group` / `config_key` decorators on dataclasses, registers them with the backend,
and keeps bound instances live through an async background task over gRPC.

!!! info "Full guide"
    The complete SDK guide — installation, transports, discovery, and binding details lives
    in the package source:
    [packages/python/README.md](https://github.com/maximiliysiss/konfigo/blob/master/packages/python/README.md)

## Quick start

```python
@config_group(key="Payments", group_name="Payments")
@dataclass
class PaymentsOptions:
    provider: str = config_key(default_value="Stripe")

client = await KonfigoClient.create(options=options, transport=transport,
                                    definitions=discover_definitions(PaymentsOptions))
client.start_background_task()
payments = bind_config(client.store.snapshot(), PaymentsOptions)
```

## See also

- [packages/python](https://github.com/maximiliysiss/konfigo/tree/master/packages/python) — SDK source
- [samples/python](https://github.com/maximiliysiss/konfigo/tree/master/samples/python) — runnable async HTTP server sample
- [gRPC Protocol — Python connection](../grpc.md#python)
