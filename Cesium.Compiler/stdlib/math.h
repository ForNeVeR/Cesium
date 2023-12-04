#pragma once
#include <stdlib.h>

#if FLT_EVAL_METHOD == 0
#define float float_t
#define double double_t
#endif

#if FLT_EVAL_METHOD == 1
#define double float_t
#define double double_t
#endif

#if FLT_EVAL_METHOD == 2
#define long double float_t
#define long double double_t
#endif
