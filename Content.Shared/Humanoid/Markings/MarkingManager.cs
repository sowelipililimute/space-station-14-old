using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Offbrand.NuBody;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed class MarkingManager
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<HumanoidVisualLayers, FrozenDictionary<string, MarkingPrototype>> _categorizedMarkings = default!;
    private FrozenDictionary<string, MarkingPrototype> _markings = default!;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypeReload;
        CachePrototypes();
    }

    private void CachePrototypes()
    {
        var markingDict = new Dictionary<HumanoidVisualLayers, Dictionary<string, MarkingPrototype>>();

        foreach (var category in Enum.GetValues<HumanoidVisualLayers>())
        {
            markingDict.Add(category, new());
        }

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            try
            {
                markingDict[prototype.BodyPart].Add(prototype.ID, prototype);
            }
            catch (Exception e)
            {
                throw new Exception($"failed to process {prototype.ID}", e);
            }
        }

        _markings = _prototype.EnumeratePrototypes<MarkingPrototype>().ToFrozenDictionary(x => x.ID);
        _categorizedMarkings = markingDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
    }

    public FrozenDictionary<string, MarkingPrototype> MarkingsByLayer(HumanoidVisualLayers category)
    {
        // all marking categories are guaranteed to have a dict entry
        return _categorizedMarkings[category];
    }

    /// <summary>
    ///     Markings by layer and group.
    /// </summary>
    /// <remarks>
    ///     This is done per layer, as enumerating over every single marking by group isn't useful.
    ///     Please make a pull request if you find a use case for that behavior.
    /// </remarks>
    /// <returns></returns>
    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByLayerAndGroup(HumanoidVisualLayers layer,
        ProtoId<MarkingsGroupPrototype> group)
    {
        var groupProto = _prototype.Index(group);
        var res = new Dictionary<string, MarkingPrototype>();
        var whitelisted = groupProto.Limits.GetValueOrDefault(layer)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;

        foreach (var (key, marking) in MarkingsByLayer(layer))
        {
            if (whitelisted && marking.GroupWhitelist == null)
            {
                continue;
            }

            if (marking.GroupWhitelist != null && !marking.GroupWhitelist.Contains(group))
            {
                continue;
            }
            res.Add(key, marking);
        }

        return res;
    }

    /// <summary>
    ///     Markings by category and sex.
    /// </summary>
    /// <remarks>
    ///     This is done per category, as enumerating over every single marking by group isn't useful.
    ///     Please make a pull request if you find a use case for that behavior.
    /// </remarks>
    /// <returns></returns>
    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByLayerAndSex(HumanoidVisualLayers layer,
        Sex sex)
    {
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByLayer(layer))
        {
            if (marking.SexRestriction != null && marking.SexRestriction != sex)
            {
                continue;
            }

            res.Add(key, marking);
        }

        return res;
    }

    /// <summary>
    ///     Markings by category, species and sex.
    /// </summary>
    /// <remarks>
    ///     This is done per category, as enumerating over every single marking by group isn't useful.
    ///     Please make a pull request if you find a use case for that behavior.
    /// </remarks>
    /// <returns></returns>
    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByLayerAndGroupAndSex(HumanoidVisualLayers layer,
        ProtoId<MarkingsGroupPrototype> group,
        Sex sex)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(layer)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByLayer(layer))
        {
            if (whitelisted && marking.GroupWhitelist == null)
            {
                continue;
            }

            if (marking.GroupWhitelist != null && !marking.GroupWhitelist.Contains(group))
            {
                continue;
            }

            if (marking.SexRestriction != null && marking.SexRestriction != sex)
            {
                continue;
            }

            res.Add(key, marking);
        }

        return res;
    }

    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
    {
        return _markings.TryGetValue(marking.MarkingId, out markingResult);
    }

    /// <summary>
    ///     Check if a marking is valid according to the category, species, and current data this marking has.
    /// </summary>
    /// <returns></returns>
    public bool IsValidMarking(Marking marking, HumanoidVisualLayers layer, ProtoId<MarkingsGroupPrototype> group, Sex sex)
    {
        if (!TryGetMarking(marking, out var proto))
        {
            return false;
        }

        if (proto.BodyPart != layer ||
            proto.GroupWhitelist != null && !proto.GroupWhitelist.Contains(group) ||
            proto.SexRestriction != null && proto.SexRestriction != sex)
        {
            return false;
        }

        return marking.MarkingColors.Count == proto.Sprites.Count;
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingPrototype>())
            CachePrototypes();
    }


    public bool CanBeApplied(ProtoId<MarkingsGroupPrototype> group, Sex sex, MarkingPrototype prototype)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(prototype.BodyPart)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;

        if (whitelisted && prototype.GroupWhitelist == null)
        {
            return false;
        }

        if (prototype.GroupWhitelist != null &&
            !prototype.GroupWhitelist.Contains(group))
        {
            return false;
        }

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    /// <summary>
    /// Ensures that the <see cref="markings"/> have a valid amount of colors
    /// </summary>
    public void EnsureValidColors(List<Marking> markings)
    {
        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (marking.Sprites.Count != markings[i].MarkingColors.Count)
            {
                markings[i] = new Marking(marking.ID, marking.Sprites.Count);
            }
        }
    }

    /// <summary>
    /// Ensures that the markings are valid per the constraints on <see cref="group"/> and <see cref="sex"/>
    /// </summary>
    public void EnsureValidGroupAndSex(List<Marking> markings, ProtoId<MarkingsGroupPrototype> group, Sex sex)
    {
        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (!CanBeApplied(group, sex, marking))
            {
                markings.RemoveAt(i);
                continue;
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markings"/> only belong to the <see cref="layers"/>
    /// </summary>
    public void EnsureValidLayers(List<Marking> markings, HashSet<HumanoidVisualLayers> layers)
    {
        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (!layers.Contains(marking.BodyPart))
            {
                markings.RemoveAt(i);
                continue;
            }
        }
    }

    /// <summary>
    /// Ensures the list of <see cref="markings"/> is valid per the limits of the <see cref="group"/>
    /// </summary>
    public void EnsureValidLimits(List<Marking> markings, ProtoId<MarkingsGroupPrototype> group, HashSet<HumanoidVisualLayers> layers, Color? skinColor, Color? eyeColor)
    {
        var groupProto = _prototype.Index(group);
        var counts = new Dictionary<HumanoidVisualLayers, int>();

        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (!groupProto.Limits.TryGetValue(marking.BodyPart, out var limit))
                continue;

            var count = counts.GetValueOrDefault(marking.BodyPart);
            if (count >= limit.Limit)
            {
                markings.RemoveAt(i);
                continue;
            }

            counts[marking.BodyPart] = counts.GetValueOrDefault(marking.BodyPart) + 1;
        }

        foreach (var layer in layers)
        {
            if (!groupProto.Limits.TryGetValue(layer, out var limit))
                continue;

            var count = counts.GetValueOrDefault(layer);
            if (count > 0 || !limit.Required)
                continue;

            foreach (var marking in limit.Default)
            {
                if (!_markings.TryGetValue(marking, out var markingProto))
                    continue;

                var colors = MarkingColoring.GetMarkingLayerColors(markingProto, skinColor, eyeColor);
                markings.Add(new(marking, colors));
            }
        }
    }

    public bool TryGetMarkingData(EntProtoId organ, [NotNullWhen(true)] out HashSet<HumanoidVisualLayers>? layers, [NotNullWhen(true)] out ProtoId<MarkingsGroupPrototype>? group)
    {
        layers = null;
        group = null;

        if (!_prototype.TryIndex(organ, out var organProto))
            return false;

        if (!organProto.TryGetComponent<OFMVisualOrganMarkingsComponent>(out var comp, _component))
            return false;

        layers = comp.Layers;
        group = comp.Group;

        return true;
    }

    public bool MustMatchSkin(string species, HumanoidVisualLayers layer, out float alpha, IPrototypeManager? prototypeManager = null)
    {
        alpha = 1;
        return true;
        /*IoCManager.Resolve(ref prototypeManager);
        var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
        if (
            !prototypeManager.Resolve(speciesProto.SpriteSet, out var baseSprites) ||
            !baseSprites.Sprites.TryGetValue(layer, out var spriteName) ||
            !prototypeManager.Resolve(spriteName, out HumanoidSpeciesSpriteLayer? sprite) ||
            sprite == null ||
            !sprite.MarkingsMatchSkin
        )
        {
            alpha = 1f;
            return false;
        }

        alpha = sprite.LayerAlpha;
        return true;*/
    }
}
