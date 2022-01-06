int main()
{
    int g = 420;
    int h = 10;
    // */ // comment, not syntax error
    int f = g/**//h; // equivalent to f = g / h;
    if (f != 42) return 100;
    //\
    i(); // part of a two-line comment
/\
/ j(); // part of a two-line comment

    int n = 2;
    int p = 40;
    int o = 1;
    int m = n//**/o
        + p; // equivalent to m = n + p;
    if (m != 42) return 100;
    return 42;
}
