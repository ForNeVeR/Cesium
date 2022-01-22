using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.TopLevel;

public interface ITopLevelNode
{
    public void Emit(TranslationUnitContext context);
}
