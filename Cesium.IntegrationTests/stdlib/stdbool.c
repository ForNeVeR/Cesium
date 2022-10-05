#include <stdbool.h>

int main(int argc, char *argv[])
{
#ifndef bool
    return 1;
#endif

#ifndef true
    return 2;
#endif

#ifndef false
    return 3;
#endif

#ifndef __bool_true_false_are_defined
    return 4;
#endif

    if (true != 1) {
        return 5;
    }

    if (false != 0) {
        return 6;
    }

    return 42;
}
