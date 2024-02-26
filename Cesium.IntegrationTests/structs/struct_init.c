typedef struct Foo
{
    int a; int b;
    struct { long _1; long _2; } inner;
    struct { long he; long ha; } other_inner;
    union { int integer; float f; };
    struct { int anon_int; };
    union { int not_anon; float its; } named_union;
    struct { struct { int level_3; } level_2; } level_1;
} Foo;
int main() {
    Foo f = { .a = 2, 2, {2,2}, {.he = 2, .ha = 2 }, .anon_int = 5, .integer = 5, .named_union.not_anon = 10, .level_1.level_2.level_3 = 10 };
    return f.a + f.b + f.inner._1 + f.inner._2 + f.other_inner.ha + f.other_inner.he + f.level_1.level_2.level_3 + f.named_union.not_anon + f.anon_int + f.integer;
}
