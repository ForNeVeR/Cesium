#pragma once
#include <stdarg.h>
#include <stddef.h>

typedef void FILE;

#define stdin 0
#define stdout 1
#define stderr 2

__cli_import("Cesium.Runtime.StdIoFunctions::PutS")
int puts(char *s);

__cli_import("Cesium.Runtime.StdIoFunctions::GetChar")
int getchar(void);

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

__cli_import("Cesium.Runtime.StdIoFunctions::FGetC")
int fgetc(FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::FGetS")
char* fgets(char* str, int count, FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::FEof")
int feof(FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::FOpen")
FILE* fopen(const char* filename, const char* mode);

__cli_import("Cesium.Runtime.StdIoFunctions::FOpenS")
errno_t fopen_s(FILE* * streamptr,
    const char* filename,
    const char* mode);

__cli_import("Cesium.Runtime.StdIoFunctions::FClose")
int fclose(FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::Rewind")
int rewind(FILE* stream);

__cli_import("Cesium.Runtime.StdIoFunctions::FError")
int ferror(FILE* stream);

#define SEEK_SET    0
#define SEEK_CUR    1

#define SEEK_END    2

#define EOF    (-1)

__cli_import("Cesium.Runtime.StdIoFunctions::FSeek")
int fseek(FILE* stream, long offset, int origin);

__cli_import("Cesium.Runtime.StdIoFunctions::PError")
void perror(const char* s);

__cli_import("Cesium.Runtime.StdIoFunctions::Remove")
int remove(const char* pathname);
