/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

/* _Thread_local at file scope: each thread gets its own copy of the variable. */

_Thread_local int tl_counter = 0;

int increment_tl(void) {
    tl_counter++;
    return tl_counter;
}

/* _Thread_local combined with static at file scope */
static _Thread_local int tl_static = 10;

int get_tl_static(void) {
    return tl_static;
}

int main(void)
{
    /* thread-local counter starts at 0 on the main thread */
    if (increment_tl() != 1) return -1;
    if (increment_tl() != 2) return -2;

    /* static _Thread_local retains its initial value on this thread */
    if (get_tl_static() != 10) return -3;

    return 42;
}
