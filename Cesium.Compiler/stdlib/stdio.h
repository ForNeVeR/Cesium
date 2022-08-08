__cli_import("Cesium.Runtime.StdIoFunctions::PutS")
void puts(char *s); // TODO[#156]: Change to int

void printf(char* s) {
    // That's temporary until varargs would not be implemented.
    // no formatting obviously.
    puts(s);
}
