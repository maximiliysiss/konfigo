from __future__ import annotations

from dataclasses import dataclass

from konfigo import ValueType, bind_config, config_group, config_key, discover_definitions


@config_group
@dataclass
class FutureOptions:
    provider: str = config_key(default_value="Stripe")
    enabled: bool = config_key(default_value="true")


def test_discover_definitions_resolves_future_annotations():
    (definition,) = discover_definitions(FutureOptions)
    options = {option.name: option for option in definition.options}

    assert options["provider"].type is ValueType.STRING
    assert options["enabled"].type is ValueType.BOOLEAN


def test_bind_config_resolves_future_annotations():
    options = bind_config(
        {
            "FutureOptions:provider": "Stripe",
            "FutureOptions:enabled": "true",
        },
        FutureOptions,
    )

    assert options.provider == "Stripe"
    assert options.enabled is True
