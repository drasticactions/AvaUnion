using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaUnion.Elements;

namespace AvaUnion.Controls;

/// <summary>
/// A button that the user must hold down for <see cref="Delay"/>. A fill shows
/// the progress. When the delay completes, <see cref="Activated"/> occurs, and
/// the button becomes checked when the user releases it. When the user
/// releases the button early, the fill goes back. A click on a checked button
/// clears it.
/// </summary>
public class DelayButton : ToggleButton
{
    /// <summary>Defines the <see cref="Delay"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> DelayProperty =
        AvaloniaProperty.Register<DelayButton, TimeSpan>(nameof(Delay), TimeSpan.FromMilliseconds(300));

    /// <summary>Defines the <see cref="Progress"/> property.</summary>
    public static readonly DirectProperty<DelayButton, double> ProgressProperty =
        AvaloniaProperty.RegisterDirect<DelayButton, double>(nameof(Progress), b => b.Progress);

    /// <summary>Defines the <see cref="Activated"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ActivatedEvent =
        RoutedEvent.Register<DelayButton, RoutedEventArgs>(nameof(Activated), RoutingStrategies.Bubble);

    private double _progress;
    private double _from;
    private double _to;
    private TimeSpan _duration;
    private TimeSpan? _start;
    private bool _animating;

    static DelayButton()
    {
        UnionHintProviders.Register<DelayButton>(
            static (element, _, hints, attributes) =>
            {
                var button = (DelayButton)element;
                if (AutoHints.HasIcon(button.Content))
                {
                    hints.Add("with-icon");
                }

                attributes.Add(new("display", AutoHints.DisplayOf(button.Content)));
            },
            ContentProperty);
    }

    /// <summary>How long the user must hold the button down. The default is 300 milliseconds.</summary>
    public TimeSpan Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    /// <summary>The progress of the delay, from 0 to 1.</summary>
    public double Progress
    {
        get => _progress;
        private set => SetAndRaise(ProgressProperty, ref _progress, value);
    }

    /// <summary>Occurs when the delay completes while the user holds the button down.</summary>
    public event EventHandler<RoutedEventArgs>? Activated
    {
        add => AddHandler(ActivatedEvent, value);
        remove => RemoveHandler(ActivatedEvent, value);
    }

    /// <inheritdoc/>
    protected override void Toggle() => SetCurrentValue(IsCheckedProperty, IsChecked != true && Progress >= 1.0);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPressedProperty)
        {
            if (IsPressed && IsChecked != true)
            {
                AnimateTo(1.0, Delay * (1.0 - Progress));
            }
            else if (!IsPressed && IsChecked != true)
            {
                if (Progress >= 1.0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (IsChecked != true && !IsPressed)
                        {
                            AnimateTo(0.0, Delay * 0.3);
                        }
                    });
                }
                else
                {
                    AnimateTo(0.0, Delay * (0.3 * Progress));
                }
            }
        }
        else if (change.Property == IsCheckedProperty)
        {
            if (IsChecked == true)
            {
                _animating = false;
                Progress = 1.0;
            }
            else
            {
                AnimateTo(0.0, Delay * (0.3 * Progress));
            }
        }
    }

    private void AnimateTo(double target, TimeSpan duration)
    {
        _from = Progress;
        _to = target;
        _duration = duration;
        _start = null;
        if (duration <= TimeSpan.Zero || TopLevel.GetTopLevel(this) is not { } top)
        {
            _animating = false;
            Finish();
            return;
        }

        if (!_animating)
        {
            _animating = true;
            top.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan time)
    {
        if (!_animating)
        {
            return;
        }

        _start ??= time;
        var t = Math.Clamp((time - _start.Value).TotalMilliseconds / _duration.TotalMilliseconds, 0.0, 1.0);
        Progress = _from + ((_to - _from) * t);
        if (t >= 1.0)
        {
            _animating = false;
            Finish();
        }
        else
        {
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
        }
    }

    private void Finish()
    {
        Progress = _to;
        if (_to >= 1.0 && IsPressed && IsChecked != true)
        {
            RaiseEvent(new RoutedEventArgs(ActivatedEvent));
        }
    }
}
