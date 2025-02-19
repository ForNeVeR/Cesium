/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef struct { int x; } foo;

int main(void)
{
  foo y;
  (&y)->x = 42;
  return (&y)->x;
}
