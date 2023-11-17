#pragma once
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
