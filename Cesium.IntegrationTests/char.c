/*
 * SPDX-FileCopyrightText: Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdio.h>

int main(int argc, char *argv[])
{
    if ('a' != 97)
    {
        return -1;
    }

    if ('\n' != 10)
    {
        return -2;
    }

    if ('\t' != 9)
    {
        return -3;
    }

    if ('\'' != 39)
    {
        return -4;
    }

    if ('\"' != 34)
    {
        return -5;
    }

    if ('\\' != 92)
    {
        return -6;
    }

    if ('\0' != 0)
    {
        return -7;
    }

    if ('\07' != 7)
    {
        return -8;
    }

    if ('\077' != 63)
    {
        return -9;
    }

    if ('\x2A' != 42)
    {
        return -10;
    }

    if (sizeof('a') != sizeof(int))
    {
        return -11;
    }

    if (sizeof(L'a') != sizeof(int))
    {
        return -12;
    }

    return 42;
}
