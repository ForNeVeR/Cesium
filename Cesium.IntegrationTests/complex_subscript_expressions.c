typedef struct { int x[10]; } foo;
int main() {
    foo a;
    a.x[0] = 42;

    return a.x[0];
}
