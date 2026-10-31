namespace LaundryLegends.Laundry
{
    /// <summary>
    /// Physical category of a laundry item. Phase 3 treats the basket as a
    /// container placeholder, not a load-grouping system.
    /// </summary>
    public enum LaundryItemKind
    {
        Basket,
        LooseLaundry,
        Bag
    }

    /// <summary>Visible processing stage for loose laundry.</summary>
    public enum LaundryStage
    {
        Dirty,
        Washed,
        Dried,
        Wrapped
    }
}
