#pragma once
/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

#include <stdlib.h>
#include <float.h>

#if FLT_EVAL_METHOD == 0
#define float_t float
#define double_t double
#endif

#if FLT_EVAL_METHOD == 1
#define float_t double
#define double_t double
#endif

#if FLT_EVAL_METHOD == 2
#define float_t long double
#define double_t long double
#endif

__cli_import("Cesium.Runtime.MathFunctions::SqrtF")
float       sqrtf(float arg);

__cli_import("Cesium.Runtime.MathFunctions::Sqrt")
double      sqrt(double arg);

__cli_import("Cesium.Runtime.MathFunctions::LogF")
float       logf(float arg);

__cli_import("Cesium.Runtime.MathFunctions::Log")
double      log(double arg);

__cli_import("Cesium.Runtime.MathFunctions::CosF")
float       cosf(float arg);

__cli_import("Cesium.Runtime.MathFunctions::Cos")
double      cos(double arg);

__cli_import("Cesium.Runtime.MathFunctions::SinF")
float       sinf(float arg);

__cli_import("Cesium.Runtime.MathFunctions::Sin")
double      sin(double arg);
