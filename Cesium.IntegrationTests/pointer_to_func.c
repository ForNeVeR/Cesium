int foo(int t)
{
    return t + 2;
}

typedef int (*foo_t)(int x);

int main(void)
{
    foo_t x = &foo;
    return 42;
}
