#pragma once

typedef void* va_list;

#define va_start(list,...) list = varargs
#define va_copy(dest, src) dest = src;
#define va_end(ap) ((void)(ap = (va_list)0))
#define va_arg(ap, T) (*(T*)((ap += 8) - 8))

