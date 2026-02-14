/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */
#include <stddef.h>

typedef struct Foo
{
    int* a;
    int b;
} Foo;
int main() {
    int c = 33;
    Foo f = { NULL, c };
    return (int)f.a + f.b + 9;
}
