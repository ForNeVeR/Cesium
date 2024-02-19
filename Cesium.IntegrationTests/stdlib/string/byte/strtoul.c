#include <errno.h>
#include <limits.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    // comment out code with -, since we have different undertanding between platforms
    //const char* p = "10 200000000000000000000000000000 30 -40 - 42";
    const char* p = "10 30 - 42";
    printf("Parsing '%s':\n", p);
    char* end = NULL;
    for (unsigned long i = strtoul(p, &end, 10);
        p != end;
        i = strtoul(p, &end, 10))
    {
        printf("'%.*s' -> ", (int)(end - p), p);
        p = end;
        if (errno == ERANGE)
        {
            errno = 0;
            printf("range error, got ");
        }
        printf("%lu\n", i);
    }
    printf("After the loop p points to '%s'\n", p);

    return 42;
}
