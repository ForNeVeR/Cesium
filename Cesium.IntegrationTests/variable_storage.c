/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

static int x = 40 + 2;

int counter() {
    static int y = 0;
    y++;
    return y;
}

int main(void)
{
    if (x != 42) return -1;
    if (counter() != 1) return -2;
    if (counter() != 2) return -3;

    return 42;
}
