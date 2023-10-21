#include <ctype.h>
#include <limits.h>
#include <stdio.h>

int main(void)
{
    unsigned char c = '\xc6'; // letter Æ in ISO-8859-1
    printf("In the default C locale, \\xc6 is %suppercase\n",
        isupper(c) ? "" : "not ");

    return 42;
}
