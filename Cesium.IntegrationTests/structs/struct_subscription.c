typedef struct
{
    int fixedArr[4];
} foo;

int main()
{
    foo x;
    x.fixedArr[3] = 42;
    return x.fixedArr[3];
}
