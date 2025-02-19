/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main(int argc, char *argv[])
{
    int a = 0;

    for (;;)
    {
        a++;

        switch (a)
        {
        case 42:
            break;
        default:
            continue;
        }

        break;
    }

    return a;
}
