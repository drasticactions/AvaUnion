using System;
using Avalonia.Controls;

namespace AvaUnion.Gallery;

public sealed record GalleryPage(string Title, Func<Control> Create)
{
    public override string ToString() => Title;
}
