using System;
using Avalonia;
using AvaUnion.Elements;

namespace AvaUnion.Presenters;

internal sealed class SlotRepaint(Visual owner)
{
    private UnionStyleSlot? _slot;
    private bool _attached;

    public void SetAttached(bool attached)
    {
        _attached = attached;
        Update();
    }

    public void Update()
    {
        var slot = _attached ? Union.GetStyleSlot(owner) : null;
        if (ReferenceEquals(slot, _slot))
        {
            return;
        }

        if (_slot is not null)
        {
            _slot.ColorsChanged -= OnColorsChanged;
        }

        _slot = slot;
        if (_slot is not null)
        {
            _slot.ColorsChanged += OnColorsChanged;
        }
    }

    private void OnColorsChanged(object? sender, EventArgs e) => owner.InvalidateVisual();
}
