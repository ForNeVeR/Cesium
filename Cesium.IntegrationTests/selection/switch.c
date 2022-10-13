#include <stdio.h>

int main(void)
{
    int x = 10;
    int t = -1;
    switch (x)
    {
    default:
        t = 42;
    }

    if (t != 42) return t;

    t = -2;
    switch (x)
    {
    case 1:
    default:
        t = 42;
    }

    if (t != 42) return t;

    t = -3;
    x = 1;
    switch (x)
    {
    case 1:
        t = 42;
    default:
        x = 2;
    }

    if (t != 42) return t;

    t = -4;
    x = 1;
    switch (x)
    {
    case 1:
        t = 42;
        break;
    default:
        t = -4;
    }

    if (t != 42) return t;

    return 42;
}
