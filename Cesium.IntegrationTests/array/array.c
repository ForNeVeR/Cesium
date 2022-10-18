int main(int argc, char *argv[])
{
    int a[10];
    a[2 - 1] = 13;
    if (a[1] != 13) {
        return -1;
    }

    return 42;
}
