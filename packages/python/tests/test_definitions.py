from dataclasses import dataclass
from datetime import datetime, timedelta
from enum import Enum

from konfigo import ValueType, config_group, config_key, discover_definitions


class TestEnum(Enum):
    A = "a"
    B = "b"


@config_group
@dataclass
class CommonOptions:
    int_value: int = config_key()
    str_value: str = config_key()
    enum_value: TestEnum = config_key()
    datetime_value: datetime = config_key()
    timedelta_value: timedelta = config_key()
    array_value: list[int] = config_key()
    json_value: dict[str, int] = config_key()
    bool_value: bool = config_key()


def test_discover_definitions_maps_supported_types_and_defaults():
    (definition,) = discover_definitions(CommonOptions)

    options = {option.name: option for option in definition.options}

    assert definition.key == "CommonOptions"
    assert options["int_value"].type is ValueType.NUMBER
    assert options["int_value"].default_value == "0"
    assert options["str_value"].type is ValueType.STRING
    assert options["str_value"].default_value == ""
    assert options["enum_value"].type is ValueType.ENUM
    assert options["enum_value"].enum_values == ("A", "B")
    assert options["datetime_value"].type is ValueType.DATE_TIME
    assert options["timedelta_value"].type is ValueType.TIME_SPAN
    assert options["array_value"].type is ValueType.ARRAY
    assert options["array_value"].default_value == "[]"
    assert options["json_value"].type is ValueType.JSON
    assert options["json_value"].default_value == "{}"
    assert options["bool_value"].type is ValueType.BOOLEAN
    assert options["bool_value"].default_value == "false"


@config_group(key="custom-key", group_name="Custom", description="Description")
@dataclass
class CustomOptions:
    value: int = config_key(name="Value", description="Option", default_value="7")


def test_custom_group_and_key_metadata():
    (definition,) = discover_definitions(CustomOptions)
    (option,) = definition.options

    assert definition.key == "custom-key"
    assert definition.name == "Custom"
    assert definition.description == "Description"
    assert option.key == "custom-key:value"
    assert option.name == "Value"
    assert option.description == "Option"
    assert option.default_value == "7"
