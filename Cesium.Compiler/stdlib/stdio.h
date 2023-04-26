__cli_import("Cesium.Runtime.StdIoFunctions::PutS")
void puts(char *s); // TODO[#156]: Change to int

__cli_import("Cesium.Runtime.StdIoFunctions::PrintF")
int printf(char* s, ...);

__cli_import("Cesium.Runtime.StdIoFunctions::PutChar")
int putchar(char _Character);

typedef void FILE;

#define stdin 0
#define stdout 1
#define stderr 2

__cli_import("Cesium.Runtime.StdIoFunctions::PutC")
int putc(char _Character, FILE* stream);
