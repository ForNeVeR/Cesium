#include <stdlib.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
    printf("\'\"\?\\\a\b\f\n\r\t\v");

    printf("\024");
    printf("\007");
    printf("\077");

    printf("\x06");
    printf("\x28");
    printf("\xF0");
    printf("\xFF");

    printf("\u2200");
    printf("\U0001f34c");

    return 42;
}
