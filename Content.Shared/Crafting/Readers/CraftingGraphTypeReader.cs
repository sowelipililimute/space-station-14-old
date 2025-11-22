using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Readers;

[TypeSerializer]
public sealed class CraftingGraphTypeReader : ITypeReader<CraftingGraphSelector, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serialization, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (node.Has("nodes"))
            return serialization.ValidateNode<CraftingGraph>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public CraftingGraphSelector Read(ISerializationManager serialization,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookContext,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<CraftingGraphSelector>? instantiationDelegate = null)
    {
        var type = typeof(CraftingGraphSelector);
        if (node.Has("nodes"))
            type = typeof(CraftingGraph);

        return (CraftingGraphSelector) serialization.Read(type, node, context)!;
    }
}
