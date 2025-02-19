/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>
#define D(x) char*t=#x;x
D(int main(int c, char** v) { printf("#include <stdio.h>\n#define D(x) char*t=#x;x\nD(%s)\n", t); return 42; })
