#pragma once
/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stddef.h>

__cli_import("Cesium.Runtime.StringFunctions::StrLen")
size_t strlen(char* str);

__cli_import("Cesium.Runtime.StringFunctions::StrCpy")
char* strcpy(char* dest, char* src);

__cli_import("Cesium.Runtime.StringFunctions::StrNCpy")
char* strncpy(char* dest, char* src, size_t count);

__cli_import("Cesium.Runtime.StringFunctions::StrCat")
char* strcat(char* dest, char* src);

__cli_import("Cesium.Runtime.StringFunctions::StrNCat")
char* strncat(char* dest, char* src, size_t count);

__cli_import("Cesium.Runtime.StringFunctions::StrNCmp")
int strncmp(const char* lhs, const char* rhs, size_t count);

__cli_import("Cesium.Runtime.StringFunctions::Memset")
void* memset(void* dest, int ch, size_t count);

__cli_import("Cesium.Runtime.StringFunctions::StrChr")
char* strchr(const char* str, int ch);

__cli_import("Cesium.Runtime.StringFunctions::StrCmp")
int strcmp(const char* lhs, const char* rhs);

__cli_import("Cesium.Runtime.StringFunctions::MemCmp")
int memcmp(const void* lhs, const void* rhs, size_t count);

__cli_import("Cesium.Runtime.StringFunctions::StrDup")
char* strdup(const char* src);

__cli_import("Cesium.Runtime.StringFunctions::StrNDup")
char* strndup(const char* src, size_t size);

__cli_import("Cesium.Runtime.StringFunctions::StrError")
char* strerror(int errnum);

__cli_import("Cesium.Runtime.StringFunctions::StrStr")
char* strstr(const char* str, const char* substr);
