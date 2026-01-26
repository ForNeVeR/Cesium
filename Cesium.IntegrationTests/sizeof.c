/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int testPrimitiveTypeSizeof() {
    int a = sizeof(int);
    if (a != 4) {
        return 1;
    }

    if (sizeof(char) != 1) {
        return 2;
    }

    if (sizeof('a') != sizeof(int)) {
        return 3;
    }

    return 0;
}

int testNamedTypeSizeof() {
    int a = 0;
    int b = sizeof(a);
    if (b != 4) {
        return 1;
    }

    return 0;
}

int testCharSizeof() {
    if (sizeof(char) != 1) {
        return 1;
    }

    return 0;
}

int testArraySizeof() {
    int x[] = { 1,2,3,4,5 };
    if (sizeof(x) != 20) {
        return 1;
    }

    return 0;
}

int y[] = { 1,2,3,4,5 };
int testGlobalArraySizeof() {
    if (sizeof(y) != 20) {
        return 1;
    }

    return 0;
}

int testArrayLength() {
    int numElements = sizeof(y) / sizeof(int);
    if (numElements != 5) {
        return 1;
    }

    return 0;
}

int testLocalEnumSizeOf() {
    enum fooLocal {
        bar
    };

    if (sizeof(enum fooLocal) != 4) {
        return 1;
    }
    return 0;
}

enum foo {
    bar
};
int testGlobalEnumSizeOf() {
    if (sizeof(enum foo) != 4) {
        return 1;
    }
    return 0;
}

int testStructSizeof() {
    struct bar {
        int x;
        int y;
    };

    int structSize = sizeof(struct bar);

    if (structSize != 8) {
        return 1;
    }

    return 0;
}

int testDirectArrSizeof() {
    if (sizeof(int[5]) != (sizeof(int) * 5)) {
        return 1;
    }
    return 0;
}

struct foobar {
    int x;
    int y;
};
int testGlobalStructSizeof() {
    int structSize = sizeof(struct foobar);

    if (structSize != 8) {
        return 1;
    }

    return 0;
}

int main(int argc, char* argv[])
{
    if (testPrimitiveTypeSizeof()) {
        return -1;
    }

    if (testNamedTypeSizeof()) {
        return -2;
    }

    if (testGlobalStructSizeof()) {
        return -3;
    }

    if (testCharSizeof()) {
        return -4;
    }

    if (testArraySizeof()) {
        return -5;
    }

    if (testGlobalArraySizeof()) {
        return -6;
    }

    if (testArrayLength()) {
        return -7;
    }

    if (testLocalEnumSizeOf()) {
        return -8;
    }

    if (testGlobalEnumSizeOf()) {
        return -9;
    }

    if (testStructSizeof()) {
        return -10;
    }

    if (testDirectArrSizeof()) {
        return -11;
    }

    return 42;
}
