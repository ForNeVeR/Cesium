#include <stdio.h>

int main(void)
{
    FILE* tmpf = fopen("example_unique_name", "w+");
    fputs("  A B\tC", tmpf);
    fputs("name [DATA_EXPUNGED] ", tmpf);
    fputs("25 54.32E-1 56789 ", tmpf);
    fputs("0xFFFD 123ABC 123 ", tmpf);

    rewind(tmpf);

    int expectedArgsConsumed = 9;

    int i, j, hex, oct;
    float exp;
    long ptr;
    char symbols[6], str1[5], str2[20];
    symbols[5] = 0;

    int result = fscanf(tmpf, " %5c %5s %s %d %f %*2d %d %p %x %o", symbols, str1, str2, &i, &exp, &j, &ptr, &hex, &oct);

    if (result != expectedArgsConsumed) return result;

    printf("Converted %d fields:\n"
            "symbols = %s\n"
            "str1 - str2 = %s - %s\n"
            "i = %d\n"
            "exp = %E\n"
            "j = %d\n"
            "ptr = %ld\n"
            "hex = %x\n"
            "oct = %o\n", result, symbols, str1, str2, i, exp, j, ptr, hex, oct);

    return 42;
}
