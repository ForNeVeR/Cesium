#pragma once
/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef long time_t;

__cli_import("Cesium.Runtime.TimeFunctions::Time")
time_t time(time_t* arg);

typedef long clock_t;

__cli_import("Cesium.Runtime.TimeFunctions::Clock")
clock_t clock(void);

#define CLOCKS_PER_SEC (__get_clocks_per_sec())
__cli_import("Cesium.Runtime.TimeFunctions::GetClocksPerSec")
long __get_clocks_per_sec(void);

