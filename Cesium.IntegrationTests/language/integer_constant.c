#include <inttypes.h>
#include <stdio.h>

int main(int argc, char *argv[])
{
    printf("123 = %d\n", 123);
    printf("0123 = %d\n", 0123);
    printf("0x123 = %d\n", 0x123);
    printf("12345678901234567890ull = %llu\n", 12345678901234567890ull);
    // the type is a 64-bit type (unsigned long long or possibly unsigned long)
    // even without a long suffix
    printf("12345678901234567890u = %"PRIu64"\n", 12345678901234567890u);

    // printf("%lld\n", -9223372036854775808); // Error:
        // the value 9223372036854775808 cannot fit in signed long long, which
        // is the biggest type allowed for unsuffixed decimal integer constant

    printf("%llu\n", -9223372036854775808ull);
    // unary minus applied to unsigned value subtracts it from 2^64,
    // this gives unsigned 9223372036854775808

    printf("%lld\n", -9223372036854775807ll - 1);
    // correct way to form signed value -9223372036854775808

    return 42;
}
