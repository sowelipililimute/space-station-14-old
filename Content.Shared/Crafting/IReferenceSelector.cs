namespace Content.Shared.Crafting;

public interface IReferenceSelector<TSelf, TReferenced> where TSelf : IReferenceSelector<TSelf, TReferenced>
{
    abstract static TSelf Make(TReferenced id);
}
