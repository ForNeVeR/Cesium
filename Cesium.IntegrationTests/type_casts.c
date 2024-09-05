#include <stddef.h>

typedef struct foo_t
{
    int bar;
} foo_t;

int foo(int a, int b)
{
    return a + b;
}

int main(void)
{
    int a = (int) 1.0;

    size_t b = foo(1, 2);
    size_t c = (foo)(1, 2);

    if (b + c != 6) return -1;

    size_t d = (size_t) a;
    size_t e = (size_t) (a + b);
    if (d + e != 5) return -2;

    int f = (int) a;
    int g = (int) (a + b);
    if (f + g != 5) return -3;

    int h = (int) (void*) a;
    int i = (int) (void*) (a + b);
    if (h + i != 5) return -4;

    // function pointer types are not supported yet?
    // int j = (int) (void (*)(int a)) a;
    // int k = (int) (void (*)(int a)) (a + b);
    // if (j + k != 5) return -5;

    int l = (int) (foo_t*) a;
    int m = (int) (foo_t*) (a + b);
    if (l + m != 5) return -6;

    int n = (int) (struct foo_t*) a;
    int o = (int) (struct foo_t*) (a + b);
    if (n + o != 5) return -7;

    // void casting - can't test, only compile
    // line below does not work due to #227
    // (void) foo;
    (void) foo(1, 2);
    (void) (1, foo(1, 2));

    unsigned x = (unsigned)2;

    return 42;
}
