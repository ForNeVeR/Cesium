#pragma once

#undef assert

#ifdef NDEBUG

#define assert(expression) ((void)0)

#else

__cli_import("Cesium.Runtime.AssertFunctions::Assert")
void _assert(
    char const* _Message,
    char const* _File,
    unsigned     _Line
);

#define assert(expression) (void)(                                        \
            (!!(expression)) ||                                           \
            (_assert((#expression), (__FILE__), (unsigned)(__LINE__)), 0) \
        )

#endif


