int main()
{
    int j = 0;
    int c = 42;

    switch(c&3) while((c-=4)>=0) {
        j++; case 3:
        j++; case 2:
        j++; case 1:
        j++; case 0:;
    }

    return j;
}
