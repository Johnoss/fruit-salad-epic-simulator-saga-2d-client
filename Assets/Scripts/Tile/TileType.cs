namespace Tile
{
    /// <summary>
    /// Serialized by value in TileConfig.asset - only ever append, never insert or reorder,
    /// or every existing tile setting silently changes fruit.
    /// </summary>
    public enum TileType
    {
        None,
        Apple,
        Melon,
        Coconut,
        Orange,
        Kiwi,
        Strawberry,
        Starfruit,
        Avocado,
        Lemon
    }
}
