using System;
using Avalonia.Controls;

namespace AvaUnion.Gallery;

public sealed record GalleryPage(string Title, Func<Control> Create, bool Scrolls = true)
{
    public Control Host() => Scrolls ? new ScrollViewer { Content = Create() } : Create();

    public override string ToString() => Title;
}
