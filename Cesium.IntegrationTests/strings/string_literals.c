#include <string.h>

const char* a = "a " "b" " c";
int testConcatenation() {
    const char* b = "a b c";

    int result = strncmp(a, b, 5);

    if (result != 0) {
        return 0;
    }

    return 1;
}

int main(int argc, char* argv[])
{
    if (!testConcatenation()) {
        return -1;
    }

    return 42;
}
