using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Readers;

[TypeSerializer]
public sealed class CraftingGraphNodeTypeReader : ITypeReader<CraftingGraphNodeSelector, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serialization, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serialization.ValidateNode<CraftingGraphNode>(node, context);
    }

    public CraftingGraphNodeSelector Read(ISerializationManager serialization,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookContext,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<CraftingGraphNodeSelector>? instantiationDelegate = null)
    {
        var type = typeof(CraftingGraphNodeSelector);
        if (node.Has("edges") || node.Has("result"))
            type = typeof(CraftingGraphNode);

        return (CraftingGraphNodeSelector) serialization.Read(type, node, context)!;
    }
}
