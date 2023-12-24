#include <stdio.h>
#include <string.h>

void demo(const char* lhs, const char* rhs, size_t sz)
{
    for (size_t n = 0; n < sz; ++n)
        putchar(lhs[n]);

    int rc = memcmp(lhs, rhs, sz);
    const char* rel = rc < 0 ? " precedes " : rc > 0 ? " follows " : " compares equal ";
    fputs(rel, stdout);

    for (size_t n = 0; n < sz; ++n)
        putchar(rhs[n]);
    puts(" in lexicographical order");
}

int main(void)
{
    char a1[] = { 'a','b','c' };
    char a2[3 /*sizeof a1*/] = {'a','b','d'};

    demo(a1, a2, 3 /*sizeof a1*/);
    demo(a2, a1, 3 /*sizeof a1*/);
    demo(a1, a1, 3 /*sizeof a1*/);

    char* test = "memory";
    char* test2 = "m";
    if (memcmp(test, test2, 1) != 0) return -1;

    return 42;
}
