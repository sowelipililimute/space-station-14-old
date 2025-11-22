using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Readers;

[TypeSerializer]
public sealed class CraftingGraphEdgeTypeReader : ITypeReader<CraftingGraphEdgeSelector, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serialization, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (node.Has("target"))
            return serialization.ValidateNode<CraftingGraphEdge>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public CraftingGraphEdgeSelector Read(ISerializationManager serialization,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookContext,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<CraftingGraphEdgeSelector>? instantiationDelegate = null)
    {
        var type = typeof(CraftingGraphEdgeSelector);
        if (node.Has("target"))
            type = typeof(CraftingGraphEdge);

        return (CraftingGraphEdgeSelector) serialization.Read(type, node, context)!;
    }
}
