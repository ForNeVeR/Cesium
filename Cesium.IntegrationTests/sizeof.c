
int testPrimitiveTypeSizeof() {
    int a = sizeof(int);
    if (a != 4) {
        return 0;
    }

    return 1;
}

int testNamedTypeSizeof() {
    int a = 0;
    int b = sizeof(a);
    if (b != 4) {
        return 0;
    }

    return 1;
}

struct foo {
    int x;
    int y;
};

int testGlobalStructSizeof() {
    int structSize = sizeof(struct foo);

    if (structSize != 8) {
        return 0;
    }

    return 1;
}

int main(int argc, char* argv[])
{
    if (!testPrimitiveTypeSizeof()) {
        return -1;
    }

    if (!testNamedTypeSizeof()) {
        return -2;
    }

    if (!testGlobalStructSizeof()) {
        return -3;
    }

    return 42;
}
