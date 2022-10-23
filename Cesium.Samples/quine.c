#include <stdio.h>
#define D(x) char*t=#x;x
D(int main(int c, char** v) { printf("#include <stdio.h>\n#define D(x) char*t=#x;x\nD(%s)\n", t); return 42; })
