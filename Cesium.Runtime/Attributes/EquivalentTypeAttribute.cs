namespace Cesium.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public class EquivalentTypeAttribute(Type equivalentType) : Attribute
{
    public Type EquivalentType { get; } = equivalentType;
}
