#ifdef __CESIUM__
#include <cesium.h>
#endif

#ifdef __CESIUM__

#pragma pinvoke("msvcrt", win_)
int win_puts(const char*);
#pragma pinvoke(end)

#pragma pinvoke("libc", unix_)
int unix_puts(const char*);
#pragma pinvoke(end)

// so that dotnet doesn't try to find functions that don't exist
void print_win() {
    win_puts("Hello cesium!");
}

void print_unix() {
    unix_puts("Hello cesium!");
}

#else
    int puts(const char*);
#endif

int main() {
#ifdef __CESIUM__
    int os = get_os();
    if (os == OS_WINDOWS) {
        print_win();
    }
    else {
        print_unix();
    }
#else
    puts("Hello cesium!"); // native C compiler
#endif
    return 42;
}
