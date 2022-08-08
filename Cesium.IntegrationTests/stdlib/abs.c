#include <math.h>

int main(int argc, char *argv[])
{
    if (abs(-42) != 42) return -1;
    if (abs(142) != 142) return -2;
    if (abs(-2147483648) != -2147483648) return -2;
    if (abs(2147483647) != 2147483647) return 3;
    return 42;
}
