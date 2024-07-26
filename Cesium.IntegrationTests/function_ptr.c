int foo(int t)
{
    return t + 2;
}

int foov(int t, ...)
{
    return t + 2;
}

typedef int (*foo_t)(int x);
typedef int (*foov_t)(int x, ...);

int main(void)
{
    foo_t x = &foo;

    return x(40);

    // TODO[#196]
    // foov_t y = &foov;
    // return (x(40) + y(40, 123, 456, "foo")) / 2;
}
