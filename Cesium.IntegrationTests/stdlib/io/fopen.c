#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    const char* fname = "unique_name.txt"; // or tmpnam(NULL);
    int is_ok = EXIT_FAILURE;

    FILE* fp = fopen(fname, "w+");
    if (!fp) {
        perror("File opening failed");
        return is_ok;
    }
    fputs("Hello, world!\n", fp);
    rewind(fp);

    int c; // note: int, not char, required to handle EOF
    while ((c = fgetc(fp)) != EOF) // standard C I/O file reading loop
        putchar(c);

    if (ferror(fp))
        puts("I/O error when reading");
    else if (feof(fp)) {
        puts("End of file is reached successfully");
        is_ok = 42;
    }

    fclose(fp);
    remove(fname);
    return is_ok;
}
