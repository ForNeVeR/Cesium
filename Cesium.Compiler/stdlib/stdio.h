#pragma once
#include <stdarg.h>

typedef void FILE;

#define stdin 0
#define stdout 1
#define stderr 2

__cli_import("Cesium.Runtime.StdIoFunctions::PutS")
int puts(char *s);

__cli_import("Cesium.Runtime.StdIoFunctions::FPutS")
int fputs(char* s, FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::PrintF")
int printf(char* s, ...);

__cli_import("Cesium.Runtime.StdIoFunctions::FPrintF")
int fprintf(FILE* stream, char* s, ...);

__cli_import("Cesium.Runtime.StdIoFunctions::FPrintF")
int vfprintf(FILE* stream, const char* format, va_list vlist);

__cli_import("Cesium.Runtime.StdIoFunctions::PutChar")
int putchar(char _Character);

__cli_import("Cesium.Runtime.StdIoFunctions::PutC")
int putc(char _Character, FILE* stream);
