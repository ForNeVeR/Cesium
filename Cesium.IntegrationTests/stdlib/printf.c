#include <stdlib.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    printf("test");

    printf("%s\n", "myNum");

    // Print variables
    int intValue = 5;
    printf("%d\n", intValue);

    float floatValue = 5.99;
    printf("%f\n", floatValue);
    double doubleValue = 2.04;
    printf("%f\n", doubleValue);

    char myLetter = 'D';
    printf("%c\n%c", myLetter, '1');

    return 42;
}
