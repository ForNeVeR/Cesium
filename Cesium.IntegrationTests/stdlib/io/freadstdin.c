/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>
#include <string.h>

int main(void)
{
    char b[100];
    const size_t ret_code = fread(b, sizeof b[0], 100, stdin); // freadstdin.in
    printf("Read %d records\n", ret_code);
    if (ret_code != 4)
    {
        return -1;
    }

    if (strncmp(b, "test", ret_code))
    {
        return -2;
    }

    return 42;
}
