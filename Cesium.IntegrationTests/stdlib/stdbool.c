/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdbool.h>

int main(int argc, char *argv[])
{
    /* Since C23, bool, true, and false are keywords rather than macros. */
#if !defined(__STDC_VERSION__) || __STDC_VERSION__ <= 201710L
#ifndef bool
    return 1;
#endif

#ifndef true
    return 2;
#endif

#ifndef false
    return 3;
#endif
#endif

#ifndef __bool_true_false_are_defined
    return 4;
#endif

    if (true != 1) {
        return 5;
    }

    if (false != 0) {
        return 6;
    }

    return 42;
}
