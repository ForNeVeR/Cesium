typedef struct Foo
{
    int b[2];
    int a;
} Foo;
int main() {
    Foo f = { { 3, 7 }, 32 };

    Foo f2 = {};
    return f.a + f.b[0] + f.b[1];
}
