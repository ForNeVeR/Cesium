using Cesium.Ast;

namespace Cesium.CodeGen.Extensions;

internal static class StatementEx
{
    public static object ToIntermediate(this Statement statement) =>
        throw new NotImplementedException($"Statement not yet supported: {statement}.");
}
