from types import SimpleNamespace

from konfigo.grpc_transport import _unwrap_string_field


class FakeMessage:
    def __init__(self, **fields):
        self._fields = set(fields)
        for name, value in fields.items():
            setattr(self, name, value)

    def HasField(self, field_name: str) -> bool:
        return field_name in self._fields


def test_unwrap_string_field_returns_none_when_wrapper_is_absent():
    message = FakeMessage()

    assert _unwrap_string_field(message, "version_id") is None


def test_unwrap_string_field_preserves_present_empty_string():
    message = FakeMessage(version_id=SimpleNamespace(value=""))

    assert _unwrap_string_field(message, "version_id") == ""
