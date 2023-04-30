#include <stddef.h>

typedef struct
{
    int bar;
    int baz;
    int qux;
} foo;

int main(int argc, char* argv[])
{
    return offsetof(foo, bar) + offsetof(foo, baz) * offsetof(foo, qux) + 10;
}
