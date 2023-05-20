int main(int argc, char *argv[])
{
    int i = 0;

    // TODO[#409]: bad SetValueExpression results in unbalanced stack
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
