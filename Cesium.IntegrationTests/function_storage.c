#include <stdio.h>

static void worker();

static void worker()
{
    printf("inside worker");
}

static void storage_declared_worker();

void storage_declared_worker()
{
    printf("inside storage_declared_worker");
}

int main(void)
{
    worker();
    storage_declared_worker();
    return 42;
}
