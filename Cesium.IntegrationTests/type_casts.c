typedef unsigned int size_t;

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

    return 42;
}
