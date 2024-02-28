#pragma once

#define OS_UNKNOWN 0
#define OS_WINDOWS 1
#define OS_LINUX 2
#define OS_OSX 3

__cli_import("Cesium.Runtime.CesiumFunctions::GetOS")
int get_os(void);
