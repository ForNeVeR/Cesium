int global = 0;

int side_effect()
{
    global++;

    return 1;
}

int main()
{
    if (side_effect() || side_effect() || side_effect())
        if (global != 1) return -1;

    if (!side_effect() && !side_effect() && !side_effect())
        if (global != 2) return -2;

    if (side_effect() && side_effect())
        if (global != 4) return -3;

    if (!side_effect() || !side_effect())
        if (global != 6) return -4;

    return 42;
}
