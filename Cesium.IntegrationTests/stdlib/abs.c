/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <math.h>
#include <stdlib.h>
#include <limits.h>

int main(int argc, char *argv[])
{
    if (abs(-42) != 42) return -1;
    if (abs(142) != 142) return -2;
    if (abs(INT_MIN) != INT_MIN) return -2;
    if (abs(INT_MAX) != INT_MAX) return 3;
    return 42;
}
