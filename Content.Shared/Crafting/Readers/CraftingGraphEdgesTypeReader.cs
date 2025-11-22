using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Readers;

[TypeSerializer]
public sealed class CraftingGraphEdgesTypeReader : ITypeReader<CraftingGraphEdgesSelector, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serialization, SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serialization.ValidateNode<List<CraftingGraphEdgeSelector>>(node, context);
    }

    public CraftingGraphEdgesSelector Read(ISerializationManager serialization,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookContext,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<CraftingGraphEdgesSelector>? instantiationDelegate = null)
    {
        var edges = (List<CraftingGraphEdgeSelector>)serialization.Read(typeof(List<CraftingGraphEdgeSelector>), node, context)!;
        return new CraftingGraphEdges() { Edges = edges };
    }
}
