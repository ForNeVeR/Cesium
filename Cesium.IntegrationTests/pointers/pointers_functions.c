typedef int (*func)(void);
typedef int (*hostFunc)(func);
typedef struct
{
    hostFunc foo;
} foo;

int main(void)
{
    foo x;
    return 42;
}
