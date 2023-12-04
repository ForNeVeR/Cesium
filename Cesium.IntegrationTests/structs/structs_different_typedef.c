struct foo { int x; };
struct bar { struct foo *x; };

int main(void) 
{ 
  struct foo x; 
  struct bar y;

  struct bar* z = &y;
  z->x = &x;
  z->x->x = 42;

  struct foo* o = &x;
  return o->x;
}
