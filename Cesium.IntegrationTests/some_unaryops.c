#include <stdio.h>

int main() {
    short c = -2;
    int x = 0;
    long long d = 0;
    printf("%d %d %d %d %d %d", c, +c, sizeof(c), sizeof(+c), sizeof(+x), sizeof(+d));
    //                          -2 -2      2           4          4            8
    return 42;
}
