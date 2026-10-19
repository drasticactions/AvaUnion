using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaUnion.Elements;
using AvaUnion.Engine;

namespace AvaUnion.Gallery.Pages;

public partial class QmlTextPage : UserControl
{
    public QmlTextPage() => AvaloniaXamlLoader.Load(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UnionEnvironment.Changed += OnUnionChanged;
        UpdateHeadings();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnionEnvironment.Changed -= OnUnionChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnUnionChanged(UnionChange change) => UpdateHeadings();

    private void UpdateHeadings()
    {
        if (!UnionEnvironment.IsReady)
        {
            return;
        }

        var size = UnionEnvironment.Engine.Fonts.General.SizeInDips * 1.35;
        this.FindControl<Label>("Heading")!.FontSize = size;
        this.FindControl<Label>("DisabledHeading")!.FontSize = size;
    }
}
