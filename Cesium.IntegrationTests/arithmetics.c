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
    return x + 2;
}
