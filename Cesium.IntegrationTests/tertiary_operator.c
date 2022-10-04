#include <stdio.h>

int main(void)
{
    // True case.
    int time = 12;
    (time < 18) ? printf("Good day.") : printf("Good evening.");

    // False case.
    time = 20;
    (time < 18) ? printf("Good day.") : printf("Good evening.");

    return 42;
}
