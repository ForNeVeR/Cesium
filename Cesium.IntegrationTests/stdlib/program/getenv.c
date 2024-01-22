#include <stdio.h>
#include <stdlib.h>

int main(void)
{
    const char* name = "PATH";
    const char* env_p1 = getenv(name);
    if (env_p1)
    {
        printf("Your %s is %s\n", name, env_p1);

        const char *env_p2 = getenv(name);
        if (env_p1 != env_p2)
        {
            return 0;
        }
    }

    return 42;
}
