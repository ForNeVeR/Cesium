#pragma once
/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef void* va_list;

#define va_start(list,...) list = __varargs
#define va_copy(dest, src) dest = src
#define va_end(ap) ((void)(ap = (va_list)0))
#define va_arg(ap, T) (*(T*)((ap += 8) - 8))

