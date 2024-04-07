#include <stdio.h>

void greet()
{
    puts("Hello World!\n");
}

int add(int a, int b)
{
    return a + b;
}

int subtract(int a, int b)
{
    return a - b;
}

float divide(int a, int b)
{
    if (b != 0)
        return (float) a / b;
    else
        return 0;
}

int multiply(int a, int b)
{
    return a * b;
}
