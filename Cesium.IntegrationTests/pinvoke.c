#define COMPILE 0
#ifndef __APPLE__
    #ifndef __MACH__
        #ifndef __linux__
            #undef COMPILE
            #define COMPILE 1
        #endif
    #endif
#endif

#if COMPILE
    #pragma pinvoke("msvcrt")
    int puts(const char*);
    #pragma pinvoke(end)
#endif

int main() {
    #if COMPILE
        puts("Hello msvcrt!");
    #endif
    return 42;
}
