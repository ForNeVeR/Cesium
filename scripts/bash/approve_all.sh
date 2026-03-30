#!/usr/bin/env bash
set -e

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)

SOLUTION_ROOT="$SCRIPT_DIR/.."

if [ ! -d "$SOLUTION_ROOT" ]; then
    echo "Error: Solution root $SOLUTION_ROOT not found." >&2
    exit 1
fi

find "$SOLUTION_ROOT" -type f -name "*.received.txt" -print0 | while IFS= read -r -d '' received; do
    verified="${received%.received.txt}.verified.txt"

    mv -f "$received" "$verified"
    echo "Approved $(basename "$verified"")
done

echo "Done!"
