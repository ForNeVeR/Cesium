#!/usr/bin/env bash
set -e

AUTOFIX=false
EXCLUDE_EXTENSIONS=(".dotsettings")

while [[ "$#" -gt 0 ]]; do
    case $1 in
    --autofix) AUTOFIX=true ;;
    *) SOURCE_ROOT="$1" ;;
    esac
    shift
done

# Autodetect git root if not provided
if [ -z "$SOURCE_ROOT" ]; then
    SOURCE_ROOT=$(git rev-parse --show-toplevel 2>/dev/null) || {
        echo "Error: Not a git repository, and no path to one provided." >&2
        exit 1
    }
fi

cd "$SOURCE_ROOT"

BOM_ERRORS=()
LINE_ENDING_ERRORS=()

FILES=$(git ls-files | grep -vE "$EXCLUDE_EXTENSIONS")

for file in $FILES; do
    [ -f "$file" ] || continue

    if grep -q $'^\\xef\\xbb\\xbf' "$file"; then
        if [ "$AUTOFIX" = true ]; then
            sed -i '1s/^\xef\xbb\xbf//' "$file"
            echo "Removed BOM from: $file"
        else
            BOM_ERRORS+=("$file")
        fi
    fi

    if grep -q $'\r' "$file"; then
        if [ "$AUTOFIX" = true ]; then
            sed -i 's/\r//g' "$file"
            echo "Fixed line endings in: $file"
        else
            LINE_ENDING_ERRORS+=("$file")
        fi
    fi
done

if [ ${#BOM_ERRORS[@]} -ne 0 ]; then
    echo -e "\nError: The following files have UTF-8 BOM:"
    printf '%s\n' "${BOM_ERRORS[@]}"
    exit 1
fi

if [ ${#LINE_ENDING_ERRORS[@]} -ne 0 ]; then
    echo -e "\nError: The following files have CRLF endings:"
    printf '%s\n' "${LINE_ENDING_ERRORS[@]}"
    exit 1
fi

echo "All files passed encoding checks"
