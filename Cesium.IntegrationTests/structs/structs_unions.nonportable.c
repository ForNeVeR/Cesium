/*
 * SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
 *
 * SPDX-License-Identifier: MIT
 */

typedef struct {
    int _1;
    struct {
        int _2a;
    };
    union {
        long _3u;
        int _4u;
    };
    union {
        long _5u;
        int _6u;
    } uni;
    struct {
        int _7;
    } s;
} foo;

int test() {
    union {
        long l;
        int i;
    } x;
    x.l = 10;
    return x.i;
}

int main() {
    if (test() != 10) {
        return -1;
    }

    foo f;
    f._1 = 2;
    f._2a = 10;
    f._3u = 10;
    f.uni._5u = 10;
    f.s._7 = 10;
    return f._1 + f._2a + f._4u + f.uni._6u + f.s._7;
}
