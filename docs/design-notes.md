Cesium Design Notes
===================
This document contains assorted design notes on various aspects of Cesium's behavior and implementation, decisions and motivation behind them.

In-Place Arrays
---------------
In-place arrays are a bit weird in their behavior in C. Here's what I had collected so far.

**When used as a local variable or a function parameter,** an in-place array is treated as a pointer to the first element of the array. In particular, its `EmitGetValue` should work the same as `EmitGetAddress`. Since the actual stored value is a pointer, we implement `EmitGetAddress` in terms of `EmitGetValue`.

**When used as a struct field,** an in-place array is a fixed array: a giant field of a fixed size. In this case, `EmitGetValue` should be implemented in terms of `EmitGetAddress`.

**When used as a global variable,** an in-place array is treated as a giant static field of a fixed size. So, it should work the same as a struct field.
