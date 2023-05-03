static int x = 40 + 2;

int counter() {
    static int y = 0;
    y++;
    return y;
}

int main(void)
{
    if (x != 42) return -1;
    if (counter() != 1) return -2;
    // I think I need https://github.com/ForNeVeR/Cesium/pull/366
    // to see whole impact how initialization can be fixed for static variables.
    //if (counter() != 2) return -3;

    return 42;
}
