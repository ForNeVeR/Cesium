
int testIntArray() {
    int a[10] = { 99, 0, 22, 17, 2, 0, };
    if (a[3] != 17) {
        return 0;
    }

    return 1;
}

int testCharArray() {
    char a[10] = { 99, 0, 22, 17, 2, 0, };
    if (a[3] != 17) {
        return 0;
    }

    return 1;
}

int testConstCharArray() {
    const char a[] = { 'A','B','C','D' };
    if (a[3] != 'D') {
        return 0;
    }

    return 1;
}

int testSCharArray() {
    signed char a[10] = { 99, 0, 22, -17, 2, 0, };
    if (a[3] != -17) {
        return 0;
    }

    if (a[4] != 2) {
        return 0;
    }

    return 1;
}

int testShortArray() {
    short a[10] = { 99, 0, 22, 17, -2, 0, };
    if (a[3] != 17) {
        return 0;
    }

    if (a[4] != -2) {
        return 0;
    }

    return 1;
}

int g_a[10] = { 99, 0, 22, 17, 2, 0, };
int testGlobalIntArray() {
    if (g_a[3] != 17) {
        return 0;
    }

    return 1;
}

int main(int argc, char *argv[])
{
    if (!testIntArray()) {
        return -1;
    }

    if (!testCharArray()) {
        return -2;
    }

    if (!testSCharArray()) {
        return -3;
    }

    if (!testGlobalIntArray()) {
        return -4;
    }

    if (!testConstCharArray()) {
        return -5;
    }

    return 42;
}
