/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <strings.h>


int testCaseInsensetiveComparison() {
    int result = strcasecmp("a b c", "A B c");
    if (result) {
        return -1;
    }

    result = strcasecmp("a b c", "A B B");
    if (result != 1) {
        return -2;
    }

    result = strcasecmp("a b c", "A b D");
    if (result != -1) {
        return -2;
    }

    return 0;
}

int main(int argc, char* argv[])
{
    if (!testConcatenation()) {
        return -1;
    }

    return 42;
}
