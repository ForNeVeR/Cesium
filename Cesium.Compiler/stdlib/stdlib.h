#define INT_MIN -2147483648
#define INT_MAX 2147483647

__cli_import("Cesium.Runtime.StdLibFunctions::Abs")
int abs(int n);

__cli_import("Cesium.Runtime.StdLibFunctions::Exit")
void exit(int code);
