/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main(int argc, char *argv[])
{
    int i = 0;
    do {
        ++i;
        if (i < 42) {
            continue;
        }
        return i;
    } while (i < 100);
    return -1;
}
