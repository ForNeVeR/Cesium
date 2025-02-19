// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Cesium.Runtime;

public static unsafe class TimeFunctions
{
    public static long Time(long* time)
    {
        var result = (DateTime.UtcNow - new DateTime(0)).TotalSeconds;
        if (time is not null)
        {
            *time = (long)result;
        }

        return (long)result;
    }

    public static long Clock()
    {
        return Stopwatch.GetTimestamp();
    }

    public static long GetClocksPerSec()
    {
        return Stopwatch.Frequency;
    }
}
