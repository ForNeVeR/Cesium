void forward_declaration_void_1();
void forward_declaration_void_2(void);

void test(void)
{
}

int function_with_parameter(int test)
{
    int y = test + 10;
    return y;
}

int function_with_variable(int y)
{
    int test = y + 10;
    return test;
}

void declaration_void(void)
{
}

int foo()
{
    declaration_void();
    forward_declaration_void_1();
    forward_declaration_void_2();
    if (function_with_parameter(10) != 20) return -1;
    if (function_with_variable(15) != 25) return -1;
    return 42;
}

void forward_declaration_void_1()
{
    // Do nothing here.
}

void forward_declaration_void_2()
{
    // Do nothing here.
}

int missing_return()
{
    // Do nothing here.
}

int main(void)
{
    return foo();
}
