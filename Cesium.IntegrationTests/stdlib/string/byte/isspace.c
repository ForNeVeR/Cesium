/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <ctype.h>
#include <limits.h>
#include <stdio.h>

int main(void)
{
    for (int ndx = 0; ndx <= UCHAR_MAX; ndx++)
        if (isspace(ndx))
            printf("0x%02x ", ndx);

    return 42;
}
