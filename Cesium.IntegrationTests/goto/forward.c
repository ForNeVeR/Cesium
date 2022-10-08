#include <stdio.h>

// function to check even or not
void checkEvenOrNot(int num)
{
    if (num % 2 == 0)
        // jump to even
        goto even;
    else
        // jump to odd
        goto odd;

even:
    printf("%d is even", num);
    // return if even
    return;
odd:
    printf("%d is odd", num);
    return; // This statement needed since control flow analysis in Cesium is broken.
}

int main() {
    int num = 26;
    checkEvenOrNot(num);
    return 42;
}
