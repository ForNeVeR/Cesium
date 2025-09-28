// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class StressTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task T_1() => DoTest(
        """        
        typedef int int32_t;
        typedef unsigned int uint32_t;
        typedef unsigned short uint16_t;
        typedef short int16_t;
        static int32_t  func_1(void)
        { /* block id: 0 */
            int32_t l_2 = 0x62D5E48D;
            int32_t l_8 = 0xE5A9864B;
            for (l_2 = (-20); (l_2 < 1); l_2 += 1)
            { /* block id: 3 */
                uint16_t l_5 = 3U;
                int32_t l_6 = 0xBA47C9D5;
                int32_t l_7 = 0x831EB239;
                l_7 = (l_2 || (l_2 && (((l_5 < (l_2 >= l_6)) && l_2) < l_5)));
                l_8 = l_2;
                l_7 = 0xEDADE098;
            }
            l_2 = (((l_8 | l_2) == (l_8 != ((int16_t)((((uint16_t)((l_2 || 0xEDB3) ^ l_8) << (uint16_t)l_2) || l_2) | l_2) - (int16_t)0x2E26))) & l_8);
            l_2 = ((int32_t)((int16_t)((uint16_t)l_2 + (uint16_t)(l_8 == ((uint32_t)l_2 << (uint32_t)l_8))) % (int16_t)l_8) - (int32_t)l_8);
            return l_8;
        }
        """
        );
}
