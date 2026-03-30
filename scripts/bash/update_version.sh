#!/usr/bin/env bash
set -e

NEW_VERSION="${1:-0.4.1}"
SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
REPO_ROOT="$SCRIPT_DIR/.."

# My respects to whoever wrote the original script in PowerShell.
# For the purpose of not needing any dependencies, we'll handle patching with regex here as well.

echo "Updating version to: $NEW_VERSION"

update_bash_file() {
    local relative_path="$1"
    local file="$REPO_ROOT/$relative_path"
    perl -i -pe "s|NEW_VERSION=\"\$\{1:-[\d.]*\}\"|NEW_VERSION=\"\$\{1:-$NEW_VERSION\}\"|g" "$file"
    echo "Updated Bash file: $relative_path"
}

update_props_file() {
    local relative_path="$1"
    local prop_name="$2"
    local file="$REPO_ROOT/$relative_path"
    perl -i -pe "s|<($prop_name)( ?.*?)>.*?</$prop_name>|<\$1\$2>$NEW_VERSION</\$1>|g" "$file"
    echo "Updated Props file: $relative_path ($prop_name)"
}

update_template_json() {
    local search_pattern="$1"
    local symbol_name="$2"

    find "$REPO_ROOT" -path "*/$search_pattern" | while read -r file; do
        perl -i -0777 -pe "s|(\"$symbol_name\":\s*\{.*?\"value\":\s*)\"[\d.]*\"|\${1}\"$NEW_VERSION\"|gs" "$file"
        echo "Updated JSON file: $(basename "$file")"
    done
}

update_bash_file 'scripts/Update-Version.sh'

update_props_file 'Directory.Build.props' 'VersionPrefix'
update_props_file 'Cesium.Sdk/Sdk/Sdk.props' 'CesiumCompilerPackageVersion'

update_template_json 'template.json' 'CesiumVersion'

echo "Successfully updated all version strings to $NEW_VERSION."
