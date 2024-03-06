typedef struct {
    union {
        int i;
        float f;
    };
} foo;

int main() {
    foo x;
    x.f = 1.5f; // 1.5f as int == 1069547520 in Net 8.0. And OutOfMem in NetFramework
    int i = x.i;
    return i - 1069547520 + 42; 
}
