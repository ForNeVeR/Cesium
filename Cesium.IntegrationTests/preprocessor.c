#include <stdio.h>
#define Y
#define Z 0

#define SINGLE_HASH_(x) # x
#define SINGLE_HASH(x) SINGLE_HASH_(x)

int main(void)
{
    printf("__TEST_DEFINE %i", __TEST_DEFINE);
#if Z
    printf("This line also does not exists");
#endif
#if defined Y
    printf("This does exists");
#endif

    printf(SINGLE_HASH(x));
    printf("line: %d file: %s ", __LINE__, __FILE__);

    return 42;
}
