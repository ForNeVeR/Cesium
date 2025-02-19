/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main(int argc, char *argv[])
{
    int i = 1;
    int j;
    switch (i - 1)
    {
        case 0: ;
            int k = 42;
            j = k;
            break;
        default:
            j = 0;
    }
    return j;
}
