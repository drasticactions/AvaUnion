namespace AvaUnion.Gallery;

internal sealed record StyleChoice(string? Id, string Label)
{
    public override string ToString() => Label;
}
