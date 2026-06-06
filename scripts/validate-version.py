#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
import tomllib
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def read_text(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def fail(message: str) -> None:
    print(message, file=sys.stderr)
    sys.exit(1)


def check_equal(name: str, actual: str | None, expected: str) -> None:
    if actual != expected:
        fail(f"{name} version '{actual}' must match VERSION '{expected}'.")


def check_contains(path: str, expected: str) -> None:
    text = read_text(path)
    if expected not in text:
        fail(f"{path} must contain '{expected}' to match VERSION.")


def main() -> int:
    version_file = ROOT / "VERSION"
    if not version_file.exists():
        fail("VERSION file is missing.")

    raw_version = version_file.read_text(encoding="utf-8")
    version_lines = raw_version.splitlines()
    if len(version_lines) != 1 or not version_lines[0].strip():
        fail("VERSION must contain exactly one non-empty version line.")

    version = version_lines[0].strip()
    if version_lines[0] != version:
        fail("VERSION must not contain leading or trailing spaces.")

    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version):
        fail("VERSION must use numeric MAJOR.MINOR.PATCH format, for example 1.2.3.")

    version_props = ET.fromstring(read_text("Version.props"))
    props = {
        child.tag: (child.text or "").strip()
        for group in version_props.findall("PropertyGroup")
        for child in group
    }
    check_equal("Version.props KonfigoVersion", props.get("KonfigoVersion"), version)

    for property_name in (
        "Version",
        "PackageVersion",
        "AssemblyVersion",
        "FileVersion",
        "InformationalVersion",
    ):
        check_equal(f"Version.props {property_name}", props.get(property_name), "$(KonfigoVersion)")

    for path in (
        "apps/backend/Directory.Build.props",
        "packages/dotnet/src/Directory.Build.props",
    ):
        xml = ET.fromstring(read_text(path))
        fallback = None
        imports_version_props = False
        for node in xml.iter():
            if node.tag == "Import" and "Version.props" in (node.attrib.get("Project") or ""):
                imports_version_props = True
            if node.tag == "KonfigoVersion":
                fallback = (node.text or "").strip()

        if not imports_version_props:
            fail(f"{path} must import Version.props.")
        check_equal(f"{path} fallback KonfigoVersion", fallback, version)

    pyproject = tomllib.loads(read_text("packages/python/pyproject.toml"))
    check_equal("packages/python/pyproject.toml", pyproject["project"]["version"], version)

    package_json = json.loads(read_text("apps/frontend/package.json"))
    check_equal("apps/frontend/package.json", package_json.get("version"), version)

    package_lock = json.loads(read_text("apps/frontend/package-lock.json"))
    check_equal("apps/frontend/package-lock.json root", package_lock.get("version"), version)
    check_equal(
        "apps/frontend/package-lock.json package root",
        package_lock.get("packages", {}).get("", {}).get("version"),
        version,
    )

    for path in ("apps/backend/Dockerfile", "apps/frontend/Dockerfile"):
        check_contains(path, f"ARG KONFIGO_VERSION={version}")

    for path in ("README.md", "docs/deployment.md"):
        check_contains(path, version)

    print(f"Version metadata is consistent: {version}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
