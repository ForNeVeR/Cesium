typedef struct { int x; } foo;

int main(void) 
{ 
  foo y; 
  (&y)->x = 42; 
  return (&y)->x; 
}
