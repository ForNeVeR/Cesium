:; # SPDX-FileCopyrightText: 2023 Maintainers of NUKE
:; #
:; # SPDX-License-Identifier: MIT
:;
:; set -eo pipefail
:; SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
:; ${SCRIPT_DIR}/build.sh "$@"
:; exit $?

@ECHO OFF
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
