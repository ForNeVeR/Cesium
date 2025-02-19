/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main(int argc, char *argv[])
{
    int i;
    for (i = 0; i < 10000; ++i)
    {
        if (i < 42)
            continue;
        return i;
    }
    return -1;
}
