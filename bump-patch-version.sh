#!/bin/sh
set -eu

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
VERSION_FILE="$ROOT_DIR/VERSION"

if [ ! -f "$VERSION_FILE" ]; then
  echo "VERSION file is missing." >&2
  exit 1
fi

old_version=$(sed -n '1p' "$VERSION_FILE")
line_count=$(awk 'END { print NR }' "$VERSION_FILE")

if [ "$line_count" -ne 1 ] || [ -z "$old_version" ]; then
  echo "VERSION must contain exactly one non-empty version line." >&2
  exit 1
fi

if ! printf '%s' "$old_version" | grep -Eq '^[0-9]+\.[0-9]+\.[0-9]+$'; then
  echo "VERSION must use numeric MAJOR.MINOR.PATCH format, for example 1.2.3." >&2
  exit 1
fi

IFS=. read -r major minor patch <<EOF
$old_version
EOF

case "$major:$minor:$patch" in
  *[!0-9:]* | :* | *:: | *:)
    echo "VERSION must use numeric MAJOR.MINOR.PATCH format, for example 1.2.3." >&2
    exit 1
    ;;
esac

new_version="$major.$minor.$((patch + 1))"

replace_all() {
  file=$1
  OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/\Q$ENV{OLD_VERSION}\E/$ENV{NEW_VERSION}/g' "$ROOT_DIR/$file"
}

replace_xml_konfigo_version() {
  file=$1
  OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/(<KonfigoVersion(?:\s+Condition="[^"]*")?>)\Q$ENV{OLD_VERSION}\E(<\/KonfigoVersion>)/$1$ENV{NEW_VERSION}$2/g' "$ROOT_DIR/$file"
}

printf '%s\n' "$new_version" > "$VERSION_FILE"

replace_xml_konfigo_version "Version.props"
replace_xml_konfigo_version "apps/backend/Directory.Build.props"
replace_xml_konfigo_version "packages/dotnet/src/Directory.Build.props"

OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/(<PackageReleaseNotes>Initial )\Q$ENV{OLD_VERSION}\E( release\.<\/PackageReleaseNotes>)/$1$ENV{NEW_VERSION}$2/g' "$ROOT_DIR/packages/dotnet/src/Directory.Build.props"

OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/^(version = ")\Q$ENV{OLD_VERSION}\E(")$/$1$ENV{NEW_VERSION}$2/m' "$ROOT_DIR/packages/python/pyproject.toml"
OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/^(\t"version": ")\Q$ENV{OLD_VERSION}\E(")/$1$ENV{NEW_VERSION}$2/m' "$ROOT_DIR/apps/frontend/package.json"
OLD_VERSION=$old_version NEW_VERSION=$new_version perl -pi -e 'if ($. <= 12) { s/^(\t"version": ")\Q$ENV{OLD_VERSION}\E(")/$1$ENV{NEW_VERSION}$2/; s/^(\t\t\t"version": ")\Q$ENV{OLD_VERSION}\E(")/$1$ENV{NEW_VERSION}$2/; }' "$ROOT_DIR/apps/frontend/package-lock.json"

OLD_VERSION=$old_version NEW_VERSION=$new_version perl -0pi -e 's/(ARG KONFIGO_VERSION=)\Q$ENV{OLD_VERSION}\E/$1$ENV{NEW_VERSION}/g' "$ROOT_DIR/apps/backend/Dockerfile" "$ROOT_DIR/apps/frontend/Dockerfile"

replace_all "README.md"
replace_all "docs/index.md"
replace_all "docs/deployment.md"

python3 "$ROOT_DIR/scripts/validate-version.py"
echo "Bumped version: $old_version -> $new_version"
