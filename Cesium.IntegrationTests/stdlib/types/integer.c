#include <stdint.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
    printf("%zu\n", sizeof(int8_t));
    printf("%zu\n", sizeof(int16_t));
    printf("%zu\n", sizeof(int32_t));
    printf("%zu\n", sizeof(int64_t));

    printf("%zu\n", sizeof(int_fast8_t));
    //printf("%zu\n", sizeof(int_fast16_t)); // Different value on Win and non-Win platforms.
    //printf("%zu\n", sizeof(int_fast32_t)); // Different value on Linux and non-Linux platforms.
    printf("%zu\n", sizeof(int_fast64_t));

    printf("%zu\n", sizeof(int_least8_t));
    printf("%zu\n", sizeof(int_least16_t));
    printf("%zu\n", sizeof(int_least32_t));
    printf("%zu\n", sizeof(int_least64_t));

    printf("%zu\n", sizeof(uint8_t));
    printf("%zu\n", sizeof(uint16_t));
    printf("%zu\n", sizeof(uint32_t));
    printf("%zu\n", sizeof(uint64_t));

    printf("%zu\n", sizeof(uint_fast8_t));
    // printf("%zu\n", sizeof(uint_fast16_t)); // Different value on Win and non-Win platforms.
    // printf("%zu\n", sizeof(uint_fast32_t)); // Different value on Linux and non-Linux platforms.
    printf("%zu\n", sizeof(uint_fast64_t));

    printf("%zu\n", sizeof(uint_least8_t));
    printf("%zu\n", sizeof(uint_least16_t));
    printf("%zu\n", sizeof(uint_least32_t));
    printf("%zu\n", sizeof(uint_least64_t));

    printf("%zu\n", sizeof(intmax_t));
    printf("%zu\n", sizeof(uintmax_t));
    return 42;
}
