/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main(int argc, char *argv[])
{
    int i = 0;
    while (i < 100) {
        ++i;
        if (i == 42) {
            break;
        }
    }

    return i;
}
