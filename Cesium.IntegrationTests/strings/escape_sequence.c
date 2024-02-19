#include <stdlib.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
    printf("\'hello, world\"\?\\\atest\b\f\n\r42\t\v");

    printf("\0");
    printf("\02");
    printf("\024");
    printf("\007");
    printf("\077");

    printf("\x06");
    printf("\x28");
    // TODO[#296]: Uncomment this.
    // printf("\xF0");
    // printf("\xFF");

    printf("\u2200");
    printf("\U0001f34c");

    return 42;
}
