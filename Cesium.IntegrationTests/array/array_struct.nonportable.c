/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef struct { int x; } foo;

int main(int argc, char *argv[])
{
    foo a[10];
    a[2 - 1].x = 42;
    return a[1].x;
}
