// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core;

public abstract class CesiumException : Exception
{
    protected CesiumException(string message) : base(message)
    {

    }
}
