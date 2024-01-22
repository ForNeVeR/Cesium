using System.Diagnostics;

namespace Cesium.Runtime;

/// <summary>
/// Functions declared in the assert.h
/// </summary>
public static unsafe class AssertFunctions
{
    public static void Assert(byte* expression, byte* file, uint line)
    {
        var expressionStr = RuntimeHelpers.Unmarshal(expression);
        var fileStr = RuntimeHelpers.Unmarshal(file);
        Debug.Fail($"Assert expression {expressionStr} failed at {fileStr} line {line}.");
    }
}
