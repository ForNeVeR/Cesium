/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdlib.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    if (system("echo test") != 0) {
        return -1;
    }

    if (system("echo \"test\"") != 0) {
        return -2;
    }

    return 42;
}
