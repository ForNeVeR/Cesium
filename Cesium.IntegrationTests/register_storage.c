/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

/* register variables behave exactly like auto at runtime; the keyword is only a hint.
   The only semantic difference (no address-of) is a compile-time constraint. */

int sum_register(int n) {
    register int i;
    register int total = 0;
    for (i = 1; i <= n; i++) {
        total += i;
    }
    return total;
}

int main(void)
{
    /* basic arithmetic via register locals */
    if (sum_register(10) != 55) return -1;

    /* register variable survives multiple assignments */
    register int x = 7;
    x = x * 6;
    if (x != 42) return -2;

    return 42;
}
