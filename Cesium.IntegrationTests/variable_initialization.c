typedef struct { int x; } foo;

int second_struct_init() {
    foo x, x2; x2.x = 42;
    return x2.x;
}

int first_init_populated() {
    int x = 42, x2;
    return x;
}

int second_init_populated() {
    int x, x2 = 42;
    return x2;
}

int both_init_populated() {
    int x = 32, x2 = 10;
    return x + x2;
}

int first_init_expression() {
    int y = 10;
    int x = 32 + y, x2;
    return x;
}

int main(void) {
    if (second_struct_init() != 42)
        return -1;
    if (first_init_populated() != 42)
        return -2;
    if (second_init_populated() != 42)
        return -3;
    if (both_init_populated() != 42)
        return -4;
    if (first_init_expression() != 42)
        return -5;
    return 42;
}
