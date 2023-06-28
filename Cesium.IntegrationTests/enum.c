enum InterestingEnum
{
    None,
    StartingValue = 100,
    AnotherValue,
    AnotherStartingValue = 200,
    DuplicateValue = 200
};
int main(void)
{
    enum InterestingEnum test = None;
    if (test != 0)
    {
        return -1;
    }

    test = StartingValue;
    if (test != 100)
    {
        return -2;
    }

    test = AnotherValue;
    if (test != 101)
    {
        return -3;
    }

    if (AnotherStartingValue != DuplicateValue)
    {
        return -4;
    }

    return 42;
}
