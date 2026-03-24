using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared.Damage.Systems;

public sealed class DamagePopupSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamagePopupComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<DamagePopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnDamageDealt(Entity<DamagePopupComponent> ent, ref DamageDealtEvent args)
    {
        var damageTotal = _damageable.GetTotalDamage(ent.Owner);
        var damageDelta = args.Damage.GetTotal();

        var msg = ent.Comp.Type switch
        {
            DamagePopupType.Delta => damageDelta.ToString(),
            DamagePopupType.Total => damageTotal.ToString(),
            DamagePopupType.Combined => damageDelta + " | " + damageTotal,
            DamagePopupType.Hit => "!",
            _ => "Invalid type",
        };

        // Turn this back into (msg, ent.Owner, args.Origin) when shooting gets predicted.
        _popupSystem.PopupPredicted(msg, ent.Owner, null);
    }

    private void OnInteractHand(Entity<DamagePopupComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.AllowTypeChange)
        {
            var next = (DamagePopupType)(((int)ent.Comp.Type + 1) % Enum.GetValues<DamagePopupType>().Length);
            ent.Comp.Type = next;
            Dirty(ent);
            _popupSystem.PopupPredicted(Loc.GetString("damage-popup-component-switched", ("setting", ent.Comp.Type)), ent.Owner, args.User);
        }
    }
}
