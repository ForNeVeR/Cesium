/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>

enum { SIZE = 5 };

int main(void)
{
    const double a[SIZE] = { 1.0, 2.0, 3.0, 4.0, 5.0 };
    printf("Array has size %ld bytes, element size: %ld\n", sizeof a, sizeof * a);
    FILE* fp = fopen("test.bin", "wb"); // must use binary mode
    fwrite(a, sizeof *a, SIZE, fp); // writes an array of doubles
    fclose(fp);
    printf("Initialization completed\n");

    double b[SIZE];
    fp = fopen("test.bin", "rb");
    const size_t ret_code = fread(b, sizeof b[0], SIZE, fp); // reads an array of doubles
    printf("Read %d records\n", ret_code);
    if (ret_code == SIZE)
    {
        printf("Array read successfully, contents:\n");
        for (int n = 0; n != SIZE; ++n)
            printf("%f ", b[n]);
        putchar('\n');
    }
    else // error handling
    {
        if (feof(fp))
            printf("Error reading test.bin: unexpected end of file\n");
        else if (ferror(fp))
            perror("Error reading test.bin");
    }

    fclose(fp);
    return 42;
}
