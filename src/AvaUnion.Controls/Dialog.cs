using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaUnion.Controls.Primitives;
using AvaUnion.Presenters;

namespace AvaUnion.Controls;

/// <summary>
/// A dialog with a title, content and standard buttons. It shows in the
/// overlay of a window with <see cref="ShowAsync"/>, or in a window of its
/// own with <see cref="ShowWindowAsync"/>. Escape rejects it, and in the
/// overlay a press outside it also rejects it.
/// </summary>
[TemplatePart("PART_ButtonBox", typeof(Panel))]
[PseudoClasses(":windowed")]
public class Dialog : ContentControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Dialog, string?>(nameof(Title));

    /// <summary>Defines the <see cref="StandardButtons"/> property.</summary>
    public static readonly StyledProperty<DialogButtons> StandardButtonsProperty =
        AvaloniaProperty.Register<Dialog, DialogButtons>(nameof(StandardButtons));

    /// <summary>Defines the <see cref="IsModal"/> property.</summary>
    public static readonly StyledProperty<bool> IsModalProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsModal));

    /// <summary>Defines the <see cref="Accepted"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> AcceptedEvent =
        RoutedEvent.Register<Dialog, RoutedEventArgs>(nameof(Accepted), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="Rejected"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> RejectedEvent =
        RoutedEvent.Register<Dialog, RoutedEventArgs>(nameof(Rejected), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ButtonClicked"/> event.</summary>
    public static readonly RoutedEvent<DialogButtonClickedEventArgs> ButtonClickedEvent =
        RoutedEvent.Register<Dialog, DialogButtonClickedEventArgs>(nameof(ButtonClicked), RoutingStrategies.Bubble);

    private const DialogButtons AcceptButtons = DialogButtons.Ok | DialogButtons.Open | DialogButtons.Save | DialogButtons.SaveAll
        | DialogButtons.Yes | DialogButtons.YesToAll | DialogButtons.Retry | DialogButtons.Ignore;

    private const DialogButtons RejectButtons = DialogButtons.Cancel | DialogButtons.Close | DialogButtons.Discard
        | DialogButtons.No | DialogButtons.NoToAll | DialogButtons.Abort;

    private readonly DialogButtonBox _buttonBox = new();
    private TaskCompletionSource<bool?>? _result;
    private Panel? _overlayHost;
    private OverlayLayer? _overlay;
    private TopLevel? _topLevel;
    private Visual? _owner;
    private Window? _window;

    static Dialog()
    {
        FocusableProperty.OverrideDefaultValue<Dialog>(true);
    }

    /// <summary>Creates the dialog.</summary>
    public Dialog() => _buttonBox.Clicked += OnButtonClicked;

    /// <summary>The title. It shows at the top of the dialog, or in the title bar of its window.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The standard buttons at the bottom of the dialog.</summary>
    public DialogButtons StandardButtons
    {
        get => GetValue(StandardButtonsProperty);
        set => SetValue(StandardButtonsProperty, value);
    }

    /// <summary>True when the overlay dims the window and blocks its input. The default is false.</summary>
    public bool IsModal
    {
        get => GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    /// <summary>True while the dialog shows.</summary>
    public bool IsOpen => _result is not null;

    /// <summary>The buttons that the dialog shows now.</summary>
    public System.Collections.Generic.IEnumerable<Button> Buttons => _buttonBox.Buttons;

    /// <summary>Occurs when the dialog is accepted.</summary>
    public event EventHandler<RoutedEventArgs>? Accepted
    {
        add => AddHandler(AcceptedEvent, value);
        remove => RemoveHandler(AcceptedEvent, value);
    }

    /// <summary>Occurs when the dialog is rejected.</summary>
    public event EventHandler<RoutedEventArgs>? Rejected
    {
        add => AddHandler(RejectedEvent, value);
        remove => RemoveHandler(RejectedEvent, value);
    }

    /// <summary>Occurs when the user clicks a standard button, before the dialog closes.</summary>
    public event EventHandler<DialogButtonClickedEventArgs>? ButtonClicked
    {
        add => AddHandler(ButtonClickedEvent, value);
        remove => RemoveHandler(ButtonClickedEvent, value);
    }

    /// <summary>
    /// Shows the dialog in the overlay of the window of <paramref name="owner"/>,
    /// centered. The task completes when the dialog closes: true when it is
    /// accepted, false when it is rejected and null when it is closed in another way.
    /// The dialog closes when <paramref name="owner"/> leaves the window.
    /// </summary>
    public Task<bool?> ShowAsync(Visual owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (_result is not null)
        {
            throw new InvalidOperationException("The dialog shows already.");
        }

        _overlay = OverlayLayer.GetOverlayLayer(owner) ?? throw new InvalidOperationException("The owner has no overlay layer.");
        PseudoClasses.Set(":windowed", false);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        var host = new Panel { Width = _overlay.Bounds.Width, Height = _overlay.Bounds.Height };
        if (IsModal)
        {
            var dimmer = new Panel { Background = Avalonia.Media.Brushes.Transparent };
            Union.SetType(dimmer, "overlay");
            Union.SetHints(dimmer, "dialog modal");
            dimmer.Children.Add(new UnionBox());
            host.Children.Add(dimmer);
        }

        host.Children.Add(this);
        _overlayHost = host;
        _overlay.Children.Add(host);
        _overlay.PropertyChanged += OnOverlayPropertyChanged;
        _owner = owner;
        _owner.DetachedFromVisualTree += OnOwnerDetached;
        _topLevel = TopLevel.GetTopLevel(owner);
        _topLevel?.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, true);
        _result = new TaskCompletionSource<bool?>();
        FocusFirst();
        return _result.Task;
    }

    /// <summary>
    /// Shows the dialog in a window of its own, modal to <paramref name="owner"/>.
    /// The task completes when the dialog closes, as for <see cref="ShowAsync"/>.
    /// </summary>
    public async Task<bool?> ShowWindowAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (_result is not null)
        {
            throw new InvalidOperationException("The dialog shows already.");
        }

        PseudoClasses.Set(":windowed", true);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _result = new TaskCompletionSource<bool?>();
        var result = _result.Task;
        _window = new Window
        {
            Title = Title,
            Content = this,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        _window.Closed += (sender, _) =>
        {
            if (ReferenceEquals(_window, sender))
            {
                _window = null;
                Finish(null);
            }
        };
        await _window.ShowDialog(owner);
        return await result;
    }

    /// <summary>Accepts and closes the dialog.</summary>
    public void Accept()
    {
        RaiseEvent(new RoutedEventArgs(AcceptedEvent));
        Finish(true);
    }

    /// <summary>Rejects and closes the dialog.</summary>
    public void Reject()
    {
        RaiseEvent(new RoutedEventArgs(RejectedEvent));
        Finish(false);
    }

    /// <summary>Closes the dialog without accepting or rejecting it.</summary>
    public void Close() => Finish(null);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_buttonBox.Parent is Panel old)
        {
            old.Children.Remove(_buttonBox);
        }

        if (e.NameScope.Find<Panel>("PART_ButtonBox") is { } slot)
        {
            slot.Children.Add(_buttonBox);
        }

        UpdateButtons();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StandardButtonsProperty)
        {
            UpdateButtons();
        }
        else if (change.Property == TitleProperty && _window is not null)
        {
            _window.Title = Title;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsOpen)
        {
            Reject();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.DialogAutomationPeer(this);

    private void UpdateButtons()
    {
        var style = Union.GetStyle(_buttonBox);
        _buttonBox.Update(StandardButtons, style?.Layout.Padding.ToThickness() ?? new Thickness(8), style?.Layout.Spacing ?? 6);
    }

    private void OnButtonClicked(DialogButtons button)
    {
        var args = new DialogButtonClickedEventArgs(button);
        RaiseEvent(args);
        if (args.Handled)
        {
            return;
        }

        if ((AcceptButtons & button) != 0)
        {
            Accept();
        }
        else if ((RejectButtons & button) != 0)
        {
            Reject();
        }
    }

    private void OnOverlayPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && _overlayHost is not null && _overlay is not null)
        {
            _overlayHost.Width = _overlay.Bounds.Width;
            _overlayHost.Height = _overlay.Bounds.Height;
        }
    }

    private void OnOwnerDetached(object? sender, VisualTreeAttachmentEventArgs e) => Close();

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsOpen && e.Source is Visual source && source != this && !this.IsVisualAncestorOf(source))
        {
            Reject();
        }
    }

    private void FocusFirst()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Focus());
    }

    private void Finish(bool? result)
    {
        var pending = _result;
        _result = null;
        pending?.TrySetResult(result);

        if (_owner is not null)
        {
            _owner.DetachedFromVisualTree -= OnOwnerDetached;
            _owner = null;
        }

        _topLevel?.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
        _topLevel = null;
        if (_overlay is not null)
        {
            _overlay.PropertyChanged -= OnOverlayPropertyChanged;
            if (_overlayHost is not null)
            {
                _overlayHost.Children.Clear();
                _overlay.Children.Remove(_overlayHost);
            }

            _overlay = null;
            _overlayHost = null;
        }

        if (_window is { } window)
        {
            _window = null;
            window.Content = null;
            window.Close();
        }
    }
}
