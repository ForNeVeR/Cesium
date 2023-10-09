#pragma once
#include <limits.h>
#include <stddef.h>

__cli_import("Cesium.Runtime.StdLibFunctions::Abs")
int abs(int n);

__cli_import("Cesium.Runtime.StdLibFunctions::Exit")
void exit(int code);

#define RAND_MAX 0x7FFFFFFF

__cli_import("Cesium.Runtime.StdLibFunctions::Rand")
int rand(void);

__cli_import("Cesium.Runtime.StdLibFunctions::SRand")
void srand(unsigned);

__cli_import("Cesium.Runtime.StdLibFunctions::System")
int system(char* command);

__cli_import("Cesium.Runtime.StdLibFunctions::Malloc")
void* malloc(size_t size);

__cli_import("Cesium.Runtime.StdLibFunctions::Free")
void free(void* ptr);

__cli_import("Cesium.Runtime.StdLibFunctions::Realloc")
void* realloc(void* ptr, size_t new_size);

__cli_import("Cesium.Runtime.StdLibFunctions::Сalloc")
void* calloc(size_t num, size_t size);

__cli_import("Cesium.Runtime.StdLibFunctions::AlignedAlloc")
void* aligned_alloc(size_t alignment, size_t size);

__cli_import("Cesium.Runtime.StdLibFunctions::Abort")
void abort(void);

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

__cli_import("Cesium.Runtime.StdLibFunctions::Atoi")
int atoi(const char* str);
