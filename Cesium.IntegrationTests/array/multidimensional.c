int main(int argc, char *argv[])
{
    int a[10][2];
    a[2 - 1][1] = 42;
    return a[1][1];
}
