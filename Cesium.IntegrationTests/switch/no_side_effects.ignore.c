int main(int argc, char *argv[])
{
    int i = 0;

    // bad SetValueExpression results in unbalanced stack (make issue for me!)
    switch (++i)
    {
        case 0:
            break;
        case 1:
            break;
        case 2:
            break;
    }

    return i * 42;
}
