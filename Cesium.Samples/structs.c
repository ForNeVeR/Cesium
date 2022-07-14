typedef struct { int x; } foo;

int main(void) 
{ 
  foo y; 
  foo *z; 
  z = &y; 
  z->x = 42; 
  return z->x; 
}
