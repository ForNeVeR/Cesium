using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.TopLevel;

public interface ITopLevelNode
{
    void EmitTo(TranslationUnitContext context);
}
