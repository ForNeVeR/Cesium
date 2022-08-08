int main(int argc, char *argv[])
{
    int a[10];
    int* b = &a[1];
    *b = 42;
    return a[1];
}
