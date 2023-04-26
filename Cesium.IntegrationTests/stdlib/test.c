#include <stdlib.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    puts("test");
    putchar('a');
    putc('b', stdout);
    int exitCode = abs(-42);
    exit(exitCode);
    char x = 'c';
    char y = '\t';
    char z = '\x32';
    char w = '\22';
    return EXIT_SUCCESS;
}
