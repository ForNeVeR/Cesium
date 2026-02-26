/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdlib.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    if (printf("test") != 4) {
        return -1;
    }

    if (printf("%s\n", "myNum") != 6) {
        return -2;
    }

    // Print variables
    int intValue = 5;
    if (printf("%d\n", intValue) != 2) {
        return -3;
    }

    if (printf("%u\n", -1) != 11) {
        return -4;
    }

    // TODO[#586]: This test is not portable: on Windows, Cesium and MSVC use different sizes for long.
    // int maxULongLengthInChars = sizeof(long) == 4 ? 10 : 20;
    // if (printf("%lu\n", -1L) != maxULongLengthInChars + 1) { // + 1 for \n
    //     return -5;
    // }

    if (printf("%i\n", -1) != 3) {
        return -6;
    }

    float floatValue = 5.99;
    if (printf("%f\n", floatValue) != 9) {
        return -7;
    }

    double doubleValue = 2.04;
    if (printf("%f\n", doubleValue) != 9) {
        return -8;
    }

    char myLetter = 'D';
    if (printf("%c\n%c\n", myLetter, '1') != 4) {
        return -9;
    }

    if (printf("%g\n", doubleValue) != 5) {
        return -10;
    }

    if (printf("%g3\n", doubleValue) != 6) {
        return -11;
    }

    double largeDoubleValue = 10300020.44;
    if (printf("%g\n", largeDoubleValue) != 9) {
        return -12;
    }

    // We cannot validate this automatically, but at least we can uncomment and check
    // that this produce appropriate results.
    //printf("%p", &doubleValue);

    return 42;
}
