//void forward_declaration_void_1();
//void forward_declaration_void_2(void);

void declaration_void(void)
{
}

int foo()
{
    declaration_void();
    //forward_declaration_void_1();
    //forward_declaration_void_2();
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

int main(void)
{
    return foo();
}
