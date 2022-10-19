#include <stdlib.h>
#include <stdio.h>

char* t = "Hello world!";
char name[13] = "StudyTonight";
//char code[10] = { 'c','o','d','e','\0' };

int main(int argc, char** argv)
{
    printf(t);
    char* y = "Hello wyrld!";
    printf(y);
    printf(name);
    char local_name[13] = "StudyTonight";
    printf(local_name);
    //printf(code);

    return 42;
}
