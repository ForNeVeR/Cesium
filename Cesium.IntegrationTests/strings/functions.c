#include <stdlib.h>
#include <string.h>
#include <stdio.h>

char* t = "Hello world!";
char cat_t[20];
char name[13] = "StudyTonight";
//char code[10] = { 'c','o','d','e','\0' };

int main(int argc, char** argv)
{
    if (strlen(t) != 12) return -1;

    char copy_t[13];
    strcpy(copy_t, t);
    if (strlen(copy_t) != 12) return -3;

    char copy_t2[13];
    strncpy(copy_t2, t, 10);
    copy_t2[10] = '\0';
    if (strlen(copy_t2) != 10) return -4;

    strcpy(cat_t, "Hello");
    strcat(cat_t, " world!");
    if (strlen(cat_t) != 12) return -5;

    strcpy(cat_t, "Hello");
    strncat(cat_t, " world!", 3);
    if (strlen(cat_t) != 8) return -6;

    const char* string = "Hello World!";
    if (strncmp(string, "Hello!", 5) != 0) return -7;
    if (strncmp(string, "Hello", 10) <= 0) return -8;
    if (strncmp(string, "Hello there", 12) >= 0) return -9;
    if (strncmp("Hello, everybody!" + 12, "Hello, somebody!" + 11, 5) != 0) return -10;
    return 42;
}
