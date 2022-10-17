int main(int argc, char *argv[])
{
    int a[10];
    int* b = &a[1];
    b = b + 1;
    b = 3 + b;
    *b = 42;
    return a[5];
}

/*
int a[10];
a[5] = 2
*(a + 20) = 2;
int x = a[5]
int x = *(a + 20)
*/
