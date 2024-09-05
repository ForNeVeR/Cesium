#include <stdio.h>

int test()
{
    int a = 0;
    if (1)
        return 3;
    else
        a = 2;
}

int main(int argc, char *argv[])
{
    if (test() != 3) return -1;

    return 42;
}
