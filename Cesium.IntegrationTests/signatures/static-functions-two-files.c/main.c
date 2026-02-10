/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

extern int worker(void);
static int function(void)
{
    return 20;
}

int main(void)
{
    return function() + worker();
}
