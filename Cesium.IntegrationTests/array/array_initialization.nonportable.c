/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */
#include <stdio.h>

int testIntArray() {
    int a[10] = { 99, 0, 22, 17, 2, 0, };
    if (a[3] != 17) {
        return 1;
    }

    return 0;
}

int testCharArray() {
    char a[10] = { 99, 0, 22, 17, 2, 0, };
    if (a[3] != 17) {
        return 1;
    }

    return 0;
}

int testConstCharArray() {
    const char a[] = { 'A','B','C','D' };
    if (a[3] != 'D') {
        return 1;
    }

    return 0;
}

int testSCharArray() {
    signed char a[10] = { 99, 0, 22, -17, 2, 0, };
    if (a[3] != -17) {
        return 1;
    }

    if (a[4] != 2) {
        return 1;
    }

    return 0;
}

int testShortArray() {
    short a[10] = { 99, 0, 22, 17, -2, 0, };
    if (a[3] != 17) {
        return 1;
    }

    if (a[4] != -2) {
        return 1;
    }

    return 0;
}

int testDoubleArray() {
    double a[10] = { 99., 0., 22., 17., -2., 0., };
    if (a[3] != 17.) {
        return 1;
    }

    if (a[4] != -2.) {
        return 1;
    }

    return 0;
}

int g_a[10] = { 99, 0, 22, 17, 2, 0, };
int testGlobalIntArray() {
    if (g_a[3] != 17) {
        return 1;
    }

    return 0;
}

int testMultidimensionalArray() {
    int a[][6] = {
        {99, 0, 22, 17, 2, 0,},
        {1,2,3,}
    };
    if (a[1][2] != 3) {
        printf("a[0][0] = %d\n", a[0][0]);
        return 1;
    }

    return 0;
}

char* global_c[10] = { "string", "other" };

static char* static_c[] = { "string", "other" };

int main(int argc, char *argv[])
{
    if (testIntArray()) {
        return -1;
    }

    if (testCharArray()) {
        return -2;
    }

    if (testSCharArray()) {
        return -3;
    }

    if (testGlobalIntArray()) {
        return -4;
    }

    if (testConstCharArray()) {
        return -5;
    }

    if (testShortArray()) {
        return -6;
    }

    if (testDoubleArray()) {
        return -7;
    }

    if (testMultidimensionalArray()) {
        return -8;
    }

    return 42;
}
