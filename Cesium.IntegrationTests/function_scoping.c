#include <stdio.h>

void worker(int x)
{
    printf("%d", x);
    {
        int x = 3;
        printf("%d", x);
    }
}
int z = 100;
void worker_global(int z)
{
    printf("%d", z);
}
int z1 = 100;
void worker_local()
{
    int z1 = 3;
    printf("%d", z);
}

int main(void)
{
    worker(11);
    worker_global(123);
    worker_local();
    return 42;
}
