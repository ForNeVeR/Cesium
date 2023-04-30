#include <stddef.h>

typedef struct
{
    int asd;
    int fgh;
} bar_t;

typedef struct foo
{
    int bar;
    bar_t baz;
    int qux;
} foo;

int main(int argc, char* argv[])
{
    // return ((size_t) (42));
    // return offsetof(foo, bar) + offsetof(foo, baz) * offsetof(foo, qux) + 10;

    return offsetof(foo, bar) + offsetof(foo, baz.fgh) * offsetof(foo, qux) - 54;
}
