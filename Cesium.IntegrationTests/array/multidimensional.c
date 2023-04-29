int b[10][10];

int main(int argc, char *argv[])
{
    int a[10][2];
    a[2 - 1][1] = 13;
    int acheck = a[1][1];
    if (acheck != 13) {
        return -1;
    }

    b[2 - 1][1] = 15;
    int bcheck = b[1][1];
    if (bcheck != 15) {
        return -2;
    }

    int i = 3;
    int j = 4;
    b[i][j] = 17;
    if (b[3][4] != 17) {
        return -2;
    }

    return 42;
}
