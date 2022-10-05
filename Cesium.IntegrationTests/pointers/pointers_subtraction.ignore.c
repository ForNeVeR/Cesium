int main(int argc, char* argv[])
{
    int a[10];
    int* b = &a[1];
    int* c = &a[8];
    return 6 * (c - b);
}
