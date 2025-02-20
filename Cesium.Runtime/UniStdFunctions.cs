// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Runtime;

public static class UniStdFunctions
{
    public static int Sleep(double duration)
    {
        Thread.Sleep((int)(duration * 1000));
        return 0;
    }
    public static int USleep(uint duration)
    {
        Thread.Sleep((int)(duration / 1000));
        return 0;
    }
}
