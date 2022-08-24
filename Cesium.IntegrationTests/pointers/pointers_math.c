int main(int argc, char *argv[])
{
    int a[10];
    int* b = &a[1];
    b = b + 1;
    b = 3 + b;
    *b = 42;
    return a[5];
}
