// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public class EquivalentTypeAttribute(Type equivalentType) : Attribute
{
    public Type EquivalentType { get; } = equivalentType;
}
