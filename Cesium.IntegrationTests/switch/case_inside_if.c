/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

int main()
{
    int j = 41;
    int c = 3;
    int altered = 0;

    switch(c) {
        case 3:
            if (altered) {
        case 2:
                j++;
            }
            break;
    }

    c = 2;
    switch (c) {
    case 3:
        if (altered) {
    case 2:
        j++;
        }
        break;
    }

    return j;
}
