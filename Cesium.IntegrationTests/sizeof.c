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

int testCharSizeof() {
    if (sizeof(char) != 1) {
        return 0;
    }

    return 1;
}

int testArraySizeof() {
    int x[] = { 1,2,3,4,5 };
    if (sizeof(x) != 20) {
        return 0;
    }

    return 1;
}

int y[] = { 1,2,3,4,5 };
int testGlobalArraySizeof() {
    if (sizeof(y) != 20) {
        return 0;
    }

    return 1;
}

int testArrayLength() {
    int numElements = sizeof(y) / sizeof(int);
    if (numElements != 5) {
        return 0;
    }

    return 1;
}

// TODO [453]: Failing 
//int testStructSizeof() {
//    struct bar {
//        int x;
//        int y;
//    };
//
//    int structSize = sizeof(struct bar);
//
//    if (structSize != 8) {
//        return 0;
//    }
//
//    return 1;
//}

// TODO [332]: Failing 
//int testStructSizeof() {
//    return sizeof(int[5]);
//}

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

    if (!testCharSizeof()) {
        return -4;
    }

    if (!testArraySizeof()) {
        return -5;
    }

    if (!testGlobalArraySizeof()) {
        return -6;
    }

    if (!testArrayLength()) {
        return -7;
    }

    return 42;
}
