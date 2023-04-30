// typedef __nint ptrdiff_t;
typedef unsigned int size_t;

typedef unsigned int max_align_t;

typedef unsigned short wchar_t;

#define NULL ((void*)0)
#define offsetof(type, member) __builtin_offsetof(type, member)
