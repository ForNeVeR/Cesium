#pragma once
/*
 * SPDX-FileCopyrightText: 2022-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stddef.h>

__cli_import("Cesium.Runtime.StringsFunctions::StrCaseCmp")
int strcasecmp(char* __s1, char* __s2);

__cli_import("Cesium.Runtime.StringsFunctions::StrNCaseCmp")
int strncasecmp(char* __s1, char* __s2, size_t __n);
