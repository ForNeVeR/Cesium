#pragma once
typedef __nint ptrdiff_t;
typedef __nuint size_t;

typedef unsigned int max_align_t;

typedef unsigned short wchar_t;

typedef int                           errno_t;
typedef size_t rsize_t;

#define NULL ((void*)0)
#define offsetof(type, member) ((size_t)((char*)&__builtin_offsetof_instance((type *)0).member - (char*)&__builtin_offsetof_instance((type *)0)))
