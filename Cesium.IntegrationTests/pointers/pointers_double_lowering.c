int main() {
    int arr[5] = {10, 20, 30, 40, 50};
    int *p = &arr[0];

    int *target = (p + 2);

    // If double-lowering occurred, target would point
    // way past the array (p + 2 * 4 * 4)
    if (*target == 30) {
        return 42;
    }

    return 1;
}
