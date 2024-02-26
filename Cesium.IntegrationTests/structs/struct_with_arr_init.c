typedef struct Foo
{
    int a;
    int b[4];
} Foo;
int main() {
    Foo f = { .b[1] = 5, .b[2] = 10, .b[3] = 25, .a = 2 };
    return f.a + f.b[0] + f.b[1] + f.b[2] + f.b[3];
}
