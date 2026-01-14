using System.Linq;
using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(Color eyeColor,
        Color skinColor,
        List<Marking> markings)
    {
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.SkinColor, new(other.Markings))
    {

    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, newColor, Markings);
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(EyeColor, SkinColor, newMarkings);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        return new(
            Color.Black,
            skinColor,
            new()
        );
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var markings = new List<Marking>();
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));
        var hairMarkingColors = new List<Color>() { newHairColor };

        if (hairStyles.Count > 0)
        {
            var newHairStyle = random.Pick(hairStyles);
            markings.Add(new(newHairStyle, hairMarkingColors));
        }

        if (facialHairStyles.Count > 0 && sex != Sex.Female)
        {
            var newFacialHairStyle = random.Pick(facialHairStyles);
            markings.Add(new(newFacialHairStyle, hairMarkingColors));
        }

        // TODO: Add random markings

        var newEyeColor = random.Pick(_realisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        return new HumanoidCharacterAppearance(newEyeColor, newSkinColor, markings);

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex)
    {
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            skinColor = strategy.EnsureVerified(skinColor);

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList());
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
