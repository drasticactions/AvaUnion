namespace AvaUnion.Presenters;

/// <summary>The resolved alignment that puts a positioned item in position.</summary>
public enum PositionerSource
{
    /// <summary>The <c>layout.alignment</c> and <c>layout.margins</c> of the item.</summary>
    Layout,

    /// <summary>The <c>icon.alignment</c> of the owner.</summary>
    Icon,

    /// <summary>The <c>text.alignment</c> of the owner.</summary>
    Text,
}
