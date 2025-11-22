using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Readers;

public abstract class ReferenceTypeReader<TSelector, TReferenceSelector, TReferenced> : ITypeReader<TSelector, ValueDataNode> where TReferenceSelector : IReferenceSelector<TReferenceSelector, TReferenced>, TSelector
{
    protected abstract Func<TReferenced, TSelector> Instantiation { get; }

    public ValidationNode Validate(ISerializationManager serialization,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return serialization.ValidateNode<TReferenced>(node, context);
    }

    public TSelector Read(ISerializationManager serialization,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TSelector>? instanceProvider = null)
    {
        var referenced = (TReferenced) serialization.Read(typeof(TReferenced), node, context)!;

        return Instantiation(referenced);
    }
}

[TypeSerializer]
public sealed class CraftingGraphReferenceTypeReader : ReferenceTypeReader<CraftingGraphSelector, CraftingGraphReference, ProtoId<CraftingGraphPrototype>>
{
    protected override Func<ProtoId<CraftingGraphPrototype>, CraftingGraphSelector> Instantiation => CraftingGraphReference.Make;
}

[TypeSerializer]
public sealed class CraftingGraphEdgeReferenceTypeReader : ReferenceTypeReader<CraftingGraphEdgeSelector, CraftingGraphEdgeReference, ProtoId<CraftingGraphEdgePrototype>>
{
    protected override Func<ProtoId<CraftingGraphEdgePrototype>, CraftingGraphEdgeSelector> Instantiation => CraftingGraphEdgeReference.Make;
}

[TypeSerializer]
public sealed class CraftingGraphNodeReferenceTypeReader : ReferenceTypeReader<CraftingGraphNodeSelector, CraftingGraphNodeReference, ProtoId<CraftingGraphNodePrototype>>
{
    protected override Func<ProtoId<CraftingGraphNodePrototype>, CraftingGraphNodeSelector> Instantiation => CraftingGraphNodeReference.Make;
}

[TypeSerializer]
public sealed class CraftingGraphConstantResultTypeReader : ReferenceTypeReader<CraftingGraphResult, ConstantCraftingResult, EntProtoId>
{
    protected override Func<EntProtoId, CraftingGraphResult> Instantiation => ConstantCraftingResult.Make;
}
