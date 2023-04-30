using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.BlockItems;

internal interface IBlockItem
{
    IBlockItem? Parent { get; set; }
    List<IBlockItem>? NextNodes { get; set; }

    IEnumerable<IBlockItem> FinalNodes(IBlockItem? unsafeNext)
    {
        if (this is ReturnStatement or CompoundTerminator)
        {
            yield return this;
            yield break;
        }

        foreach (var next in NextNodes!)
        {
            Console.WriteLine(next.GetType().Name);
            if (next is ReturnStatement or CompoundTerminator || next.NextNodes?.Count == 0)
            {
                yield return next;
                yield break;
            }

            // todo remove ???
            if (next.NextNodes == null && unsafeNext != null)
            {
                yield return unsafeNext;
                yield break;
            }

            foreach (var finalNode in next.FinalNodes(unsafeNext))
            {
                yield return finalNode;
            }
        }
    }

    void ResolveParents(IBlockItem root)
    {
        foreach (var child in GetChildren(root))
        {
            child.Parent = this;
        }
    }

    IBlockItem NextNode(IBlockItem child)
    {
        throw new NotImplementedException($"{GetType().Name}");
    }

    void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        throw new NotImplementedException($"{GetType().Name}");
    }

    bool CheckNextNodes(bool isReturnRequired, List<IBlockItem>? path = null)
    {
        if (NextNodes == null)
            throw new Exception("NextNodes are unresolved");

        if (NextNodes.Count == 0)
        {
            if (!isReturnRequired || this is ReturnStatement)
            {
                return this is ReturnStatement;
            }

            throw new Exception("Return required");
        }

        if (path != null && path.Contains(this))
        {
            throw new Exception("Loop occurred");
        }

        var result = true;

        foreach (var nextNode in NextNodes)
        {
            result &= nextNode.CheckNextNodes(isReturnRequired, new List<IBlockItem>(path ?? new()) { this });
        }

        return result;
    }

    IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        throw new NotImplementedException($"{GetType().Name}");
    }

    [Obsolete]
    bool HasDefiniteReturn => false;

    IBlockItem Lower(IDeclarationScope scope);
    void EmitTo(IEmitScope scope);
}
