int main(int argc, char *argv[])
{
    int i;
    int j = 0;
    for (i = 0; i < 42; ++i) {
        int k = 1;
        j += k;
    }
    return j;
}
