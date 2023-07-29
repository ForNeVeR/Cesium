#include <stdio.h>
#define Y
#define Z 0

int main(void)
{
    printf("__TEST_DEFINE %i", __TEST_DEFINE);
#if Z
    printf("This line also does not exists");
#endif
#if defined Y
    printf("This does exists");
#endif

    return 42;
}
