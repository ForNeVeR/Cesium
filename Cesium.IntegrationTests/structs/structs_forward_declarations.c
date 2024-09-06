typedef struct FwDeclared FwDeclared;

typedef struct Decl Decl;
struct Decl {
    int x;
};

struct FwDeclared {
    Decl var;
};

int main(void) 
{
  struct FwDeclared fw;
  return 42;
}
