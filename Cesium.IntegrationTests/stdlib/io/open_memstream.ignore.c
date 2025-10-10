/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>
#include <stdlib.h>

int main(void) {
    FILE* stream;
    char* buffer;
    size_t size;

    // Open a memory stream
    stream = open_memstream(&buffer, &size);
    if (stream == NULL) {
        perror("open_memstream");
        exit(EXIT_FAILURE);
    }

    // Write data to the stream
    fprintf(stream, "Hello, ");
    fprintf(stream, "world!");

    // Flush the stream to update the buffer and size
    fflush(stream);
    printf("Buffer: %s\n", buffer);
    printf("Size: %zu\n", size);

    // Close the stream and free the buffer
    fclose(stream);
    free(buffer);

    return 42;
}
