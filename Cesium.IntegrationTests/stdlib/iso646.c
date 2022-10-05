#include <iso646.h>

int main(int argc, char *argv[])
{
    int x = 15;
    int y = 31;
    if ((x == 0) and (y == 0)) {
        return 1;
    }

    if ((x not_eq 15) or (y not_eq 31)) {
        return 2;
    }

    if ((x bitand y) not_eq 15) {
        return 3;
    }

    if ((x bitor y) not_eq 31) {
        return 4;
    }

    if ((x xor y) not_eq 16) {
        return 5;
    }

    int z = y;
    z and_eq x;
    if (z not_eq 15) {
        return 6;
    }

    z = y;
    z or_eq x;
    if (z not_eq 31) {
        return 7;
    }

    z = y;
    z xor_eq x;
    if (z not_eq 16) {
        return 8;
    }

    if ((compl x) not_eq -16) {
        return 9;
    }

    return 42;
}
