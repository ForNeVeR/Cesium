#include <stdio.h>

int main(int argc, char *argv[])
{
    float i = 0;
    int step = 7;
    while (i < 42) {
        printf("%f", i);
        i = i + step;
    }
    return (int)i;
}
