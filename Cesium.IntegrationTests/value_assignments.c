/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main()
{
    int i = 0;

    int j = i++;

    int a = 10;
    if (a++ != 10) return -1;
    if (++a != 12) return -2;

    return i += 41;
}
