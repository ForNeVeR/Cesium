typedef struct { int x; } foo;
typedef struct { foo x; } bar;

int main(void)
{
  bar y;

  bar* z = &y;
  z->x.x = 42;

  return y.x.x;
}
