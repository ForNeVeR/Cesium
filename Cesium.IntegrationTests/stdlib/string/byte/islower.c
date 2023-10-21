#include <ctype.h>
#include <limits.h>
#include <stdio.h>

int main(void)
{
    unsigned char c = '\xe5'; // letter å in ISO-8859-1
    printf("In the default C locale, \\xe5 is %slowercase\n",
        islower(c) ? "" : "not ");

    return 42;
}
