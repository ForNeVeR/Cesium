int main(void)
{
    int y = -42;
    if (y - 3 != -45) return 0;
    y -= 10;
    if (y != -52) return -1;
    int x = 18;
    x += 1;
    ++x;
    x *= 2;

    int z = 100;
    if (z / 2 != 50) {
        return -2;
    }

    --z;
    if (z != 99) {
        return -3;
    }

    return x + 2;
}
