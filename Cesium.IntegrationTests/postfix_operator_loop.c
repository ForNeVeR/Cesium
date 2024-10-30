#include <stdio.h>
#include <string.h>

int pos = -1;

char get_next_char(char *stream)
{
    return stream[++pos];
}

int main(int argc, char *argv[])
{
    char *stream = "Hello, Cesium!\n";

    int i, c;
    char s[15];
    for (i = 0; (c = get_next_char(stream)) != '\n'; i++)
    {
        s[i] = c;
        int a = i - 1;
    }
    s[i] = '\0';

    if (i != strlen(&s[0]))
    {
        return -1;
    }

    if (strncmp(&s[0], stream, strlen(&s[0])) != 0)
    {
        return -2;
    }

    printf("%s - %i", s, strlen(&s[0]));

    return 42;
}
