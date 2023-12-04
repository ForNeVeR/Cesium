#include <stddef.h>

typedef struct { int quux; } qux_t;
typedef struct { int bar, baz; qux_t qux; } foo_t;

int main(int argc, char* argv[])
{
    foo_t foo;
    *(int*)((char*) &foo + offsetof(foo_t, qux.quux)) = 42;

    return foo.qux.quux;
}
