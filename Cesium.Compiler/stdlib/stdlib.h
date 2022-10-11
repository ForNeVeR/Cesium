#include <limits.h>

__cli_import("Cesium.Runtime.StdLibFunctions::Abs")
int abs(int n);

__cli_import("Cesium.Runtime.StdLibFunctions::Exit")
void exit(int code);

#define RAND_MAX 0x7FFFFFFF

__cli_import("Cesium.Runtime.StdLibFunctions::Rand")
int rand(void);

__cli_import("Cesium.Runtime.StdLibFunctions::SRand")
void srand(unsigned);
