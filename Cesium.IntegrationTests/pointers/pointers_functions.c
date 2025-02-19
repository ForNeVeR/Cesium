/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef int (*func)(void);
typedef int (*hostFunc)(func);
typedef struct
{
    hostFunc foo;
} foo;

int main(void)
{
    foo x;
    return 42;
}
