#include <stdlib.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    printf("test");

    printf("%s\n", "myNum");

    // Print variables
    int intValue = 5;
    printf("%d\n", intValue);
    printf("%u\n", -1);
    printf("%lu\n", -1);
    printf("%i\n", -1);

    float floatValue = 5.99;
    printf("%f\n", floatValue);
    double doubleValue = 2.04;
    printf("%f\n", doubleValue);

    char myLetter = 'D';
    printf("%c\n%c", myLetter, '1');
    // We cannot validate this automatically, but at least we can uncomment and check
    // that this produce appropriate results.
    //printf("%p", &doubleValue);

    return 42;
}
