using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.TopLevel;

public interface ITopLevelNode
{
    public void EmitTo(TranslationUnitContext context);
}
