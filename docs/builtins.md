Built-in Functions
------------------

As a C implementation, Cesium implements some helper functions for the compiler. These are built-in language constructs.

- `__builtin_offsetof_instance(pointer)`: this function is only concerned about the _type_ of the passed `pointer`. It will find the first variable of same type in the current scope (or emit a new one), and take address of said variable, as part of the `offsetof` macro implementation.
