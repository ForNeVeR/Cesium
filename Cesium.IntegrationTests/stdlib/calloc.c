#include <stdio.h>   
#include <stdlib.h> 

// This is trimmed version of https://en.cppreference.com/w/c/memory/malloc
// would be great to make it run fully.
int main()
{
    int* p1 = calloc(4, 4);  // allocates enough for an array of 4 int

    if (p1) {
        int n;
        for (n = 0; n < 4; ++n) // print it back out
            printf("p1[%d] == %d\n", n, p1[n]);
    }

    free(p1);

    return 42;
}
