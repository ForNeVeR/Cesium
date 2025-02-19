/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    FILE* tmpf = fopen("fgets_unique_name.txt", "w+"); // or tmpnam(NULL);
    fputs("Alan Turing\n", tmpf);
    fputs("John von Neumann\n", tmpf);
    fputs("Alonzo Church\n", tmpf);

    rewind(tmpf);

    char buf[8];
    while (fgets(buf, sizeof buf, tmpf) != NULL)
        printf("\"%s\"\n", buf);

    if (feof(tmpf))
        puts("End of file reached");
    return 42;
}
