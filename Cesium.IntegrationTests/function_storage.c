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

void storage_defined_worker();

static void storage_defined_worker()
{
    printf("inside storage_defined_worker");
}

int main(void)
{
    worker();
    storage_declared_worker();
    storage_defined_worker();
    return 42;
}
