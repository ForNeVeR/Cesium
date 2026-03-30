#!/usr/bin/env bash
set -e

REF_NAME="$1"
SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
REPO_ROOT="$SCRIPT_DIR/.."

if [[ "$REF_NAME" =~ ^refs/tags/v ]]; then
    VERSION="${REF_NAME#refs/tags/v}"
    echo "Determining version from ref \"$REF_NAME\"..." >&2
    echo "Pushed ref is a version tag, version: $VERSION" >&2
else
    PROPS_FILE="$REPO_ROOT/Directory.Build.props"
    echo "Determining version from $PROPS_FILE..." >&2

    VERSION=$(grep "<VersionPrefix>" "$PROPS_FILE" | sed -E 's/.*<VersionPrefix>(.*)<\/VersionPrefix>.*/\1/' | xargs)

    if [ -z "$VERSION" ]; then
        echo "Error: Could not find VersionPrefix in $PROPS_FILE" >&2
        exit 1
    fi

    echo "Pushed ref is not a version tag, got version from $PROPS_FILE: $VERSION" >&2
fi

echo "$VERSION"
