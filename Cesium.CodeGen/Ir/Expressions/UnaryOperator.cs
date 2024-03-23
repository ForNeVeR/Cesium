namespace Cesium.CodeGen.Ir.Expressions;

public enum UnaryOperator
{
    Negation, // -
    Promotion, // +
    BitwiseNot, // ~
    LogicalNot, // !
    AddressOf, // &
    Indirection, // *
}
