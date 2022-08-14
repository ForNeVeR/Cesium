#include <stdlib.h>
#include <stdio.h>

void f1(void)
{
    puts("f1");
}

void f2(void)
{
    puts("f2");
}

int main(void)
{
    if (!atexit(f1) && !atexit(f2) && !atexit(f2))
        return 42;

    // atexit registration failed
    return -1;
}  // <- if registration was successful calls f2, f2, f1
