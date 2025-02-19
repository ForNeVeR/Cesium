/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

extern int foo(int t);

int foo(int t)
{
    return t + 2;
}

int main(void)
{
    return foo(40);
}
