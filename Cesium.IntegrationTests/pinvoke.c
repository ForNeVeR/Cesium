#pragma pinvoke("msvcrt")
int puts(const char*);
#pragma pinvoke(end)

int main() {
#if __APPLE__
    return 42;
#elif unix
    return 42;
#endif
    puts("Hello msvcrt!");
    return 42;
}
