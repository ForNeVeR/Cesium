typedef unsigned int size_t;

int foo(int a, int b)
{
    return a + b;
}

int main(void)
{
    int a = (int) 1.0;
    size_t b = foo(1, 2); // ((size_t)) a;
    size_t c = (foo)(1, 2); // ((size_t)) a;
    size_t d = (size_t) a;
    size_t e = (size_t) (a + b);

    return 30 + a + b + c + e + d;
}
