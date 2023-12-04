#include <stdio.h>

void goodDay()
{
    printf("Good day.");
}
void badDay()
{
    printf("Good evening.");
}

int main(void)
{
    // True case.
    int time = 12;
    (time < 18) ? printf("Good day.") : printf("Good evening.");
    (time < 18) ? 5 : 4;

    // False case.
    time = 20;
    (time < 18) ? printf("Good day.") : printf("Good evening.");

    // True case.
    time = 12;
    (time < 18) ? goodDay() : badDay();

    // False case.
    time = 20;
    (time < 18) ? goodDay() : badDay();

    return 42;
}
