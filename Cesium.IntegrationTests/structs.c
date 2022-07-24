typedef struct { int x; } foo;
typedef struct { foo *x; } bar;

int main(void) 
{ 
  foo x; 
  bar y;

  bar* z = &y;
  z->x = &x;
  z->x->x = 42;

  foo* o = &x;
  return o->x;
}