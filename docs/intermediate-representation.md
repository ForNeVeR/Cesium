<!--
SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>

SPDX-License-Identifier: MIT
-->

Compiler's intermediate representation
=================================

Currently compiler has 3 logical representations:

- The AST which is abstract representation of parsed code with 
attached source code locations.
- High level IR (HIR) which is simplified version of AST upon which we perform analysis and code transformations
- Low level IR (LIR) which is close to machine code and used for optimizations and code generation.
  Low level IR applicable only within function body.

High level and Low level IRs are defined in the `Cesium.CodeGen/Ir` directory. Each node represented by class
implementing marker `IBlockItem` interface.

Low level IR (LIR)
--------------------

Currently LIR consists of the following classes.

- Compound statment *(reused from HIR)*
- Goto statement *(reused from HIR)*
- Label without statements attached (LabeledNopStatement)
- Conditional goto statement (ConditionalGotoStatement)
- Return statement *(reused from HIR)*
- Expression statement (ExpressionStatement)
