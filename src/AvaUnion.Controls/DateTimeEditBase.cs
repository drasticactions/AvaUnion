using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaUnion.Elements;

namespace AvaUnion.Controls;

/// <summary>
/// A spin box that edits a date, a time or both, one section at a time. The
/// section at the caret, for example the hour, steps with the arrow buttons
/// and the Up and Down keys. Typed text is read when the field loses focus
/// or the user presses Enter.
/// </summary>
[TemplatePart("PART_Spinner", typeof(ButtonSpinner))]
[TemplatePart("PART_TextBox", typeof(TextBox))]
public abstract class DateTimeEditBase : TemplatedControl
{
    /// <summary>Defines the <see cref="Format"/> property.</summary>
    public static readonly StyledProperty<string?> FormatProperty =
        AvaloniaProperty.Register<DateTimeEditBase, string?>(nameof(Format));

    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly DirectProperty<DateTimeEditBase, string> TextProperty =
        AvaloniaProperty.RegisterDirect<DateTimeEditBase, string>(nameof(Text), e => e.Text);

    private readonly List<Section> _sections = [];
    private ButtonSpinner? _spinner;
    private TextBox? _textBox;
    private string _text = string.Empty;
    private int _current;

    static DateTimeEditBase()
    {
        FocusableProperty.OverrideDefaultValue<DateTimeEditBase>(false);
        UnionHintProviders.Register<DateTimeEditBase>(
            static (_, _, hints, _) =>
            {
                hints.Add("editable");
                hints.Add("constrained");
            });
    }

    /// <summary>
    /// A custom .NET date and time format, for example <c>HH:mm</c>. Null
    /// uses the short format of the current culture, with the year digits
    /// that Qt shows for that culture.
    /// </summary>
    public string? Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <summary>The text that the field shows for the value.</summary>
    public string Text
    {
        get => _text;
        private set => SetAndRaise(TextProperty, ref _text, value);
    }

    /// <summary>The section that the arrows step, from 0 at the start of the text.</summary>
    public int CurrentSection => _current;

    /// <summary>The format in effect.</summary>
    protected string EffectiveFormat => string.IsNullOrEmpty(Format) ? DefaultFormat : Format;

    /// <summary>The format when <see cref="Format"/> is null.</summary>
    protected abstract string DefaultFormat { get; }

    /// <summary>The value as a date and time, or null when the field is empty.</summary>
    protected abstract DateTime? DateTimeValue { get; set; }

    /// <summary>Steps the section at the caret by <paramref name="steps"/>.</summary>
    public void Step(int steps)
    {
        if (DateTimeValue is not { } value || _sections.Count == 0)
        {
            return;
        }

        var section = _sections[Math.Clamp(_current, 0, _sections.Count - 1)];
        DateTimeValue = StepField(value, section.Field, steps);
        SelectSection();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_spinner is not null)
        {
            _spinner.Spin -= OnSpin;
        }

        if (_textBox is not null)
        {
            _textBox.LostFocus -= OnTextLostFocus;
            _textBox.KeyDown -= OnTextKeyDown;
            _textBox.PropertyChanged -= OnTextBoxPropertyChanged;
        }

        _spinner = e.NameScope.Find<ButtonSpinner>("PART_Spinner");
        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        if (_spinner is not null)
        {
            _spinner.Spin += OnSpin;
        }

        if (_textBox is not null)
        {
            _textBox.LostFocus += OnTextLostFocus;
            _textBox.KeyDown += OnTextKeyDown;
            _textBox.PropertyChanged += OnTextBoxPropertyChanged;
        }

        Render();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FormatProperty)
        {
            Render();
        }
    }

    /// <summary>Shows the value again. Derived classes call it when the value changes.</summary>
    protected void Render()
    {
        _sections.Clear();
        var value = DateTimeValue;
        var builder = new System.Text.StringBuilder();
        foreach (var token in Tokenize(EffectiveFormat))
        {
            if (token.Field is { } field && value is { } v)
            {
                var text = v.ToString(token.Pattern.Length == 1 ? "%" + token.Pattern : token.Pattern, CultureInfo.CurrentCulture);
                _sections.Add(new Section(field, builder.Length, text.Length));
                builder.Append(text);
            }
            else if (value is not null)
            {
                builder.Append(token.Pattern);
            }
        }

        Text = builder.ToString();
        if (_textBox is not null)
        {
            _textBox.Text = Text;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.DateTimeEditAutomationPeer(this);

    internal bool TryCommit(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            DateTimeValue = null;
            return true;
        }

        if (DateTime.TryParseExact(text, EffectiveFormat, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            DateTimeValue = parsed;
            return true;
        }

        Render();
        return false;
    }

    private static DateTime StepField(DateTime value, Field field, int steps)
    {
        switch (field)
        {
            case Field.Year:
                var year = Math.Clamp(value.Year + steps, 1, 9999);
                return new DateTime(year, value.Month, Math.Min(value.Day, DateTime.DaysInMonth(year, value.Month))).Add(value.TimeOfDay);
            case Field.Month:
                var month = Math.Clamp(value.Month + steps, 1, 12);
                return new DateTime(value.Year, month, Math.Min(value.Day, DateTime.DaysInMonth(value.Year, month))).Add(value.TimeOfDay);
            case Field.Day:
                var day = Math.Clamp(value.Day + steps, 1, DateTime.DaysInMonth(value.Year, value.Month));
                return new DateTime(value.Year, value.Month, day).Add(value.TimeOfDay);
            case Field.Hour:
                return value.Date + new TimeSpan(Math.Clamp(value.Hour + steps, 0, 23), value.Minute, value.Second);
            case Field.Minute:
                return value.Date + new TimeSpan(value.Hour, Math.Clamp(value.Minute + steps, 0, 59), value.Second);
            case Field.Second:
                return value.Date + new TimeSpan(value.Hour, value.Minute, Math.Clamp(value.Second + steps, 0, 59));
            case Field.AmPm:
                var hour = value.Hour < 12 ? (steps > 0 ? value.Hour + 12 : value.Hour) : (steps < 0 ? value.Hour - 12 : value.Hour);
                return value.Date + new TimeSpan(hour, value.Minute, value.Second);
            default:
                return value;
        }
    }

    private static IEnumerable<(string Pattern, Field? Field)> Tokenize(string format)
    {
        var i = 0;
        while (i < format.Length)
        {
            var c = format[i];
            if (c is '\'' or '"')
            {
                var end = format.IndexOf(c, i + 1);
                end = end < 0 ? format.Length : end;
                yield return (format.Substring(i + 1, end - i - 1), null);
                i = end + 1;
                continue;
            }

            if (c == '\\' && i + 1 < format.Length)
            {
                yield return (format[i + 1].ToString(), null);
                i += 2;
                continue;
            }

            var run = 1;
            while (i + run < format.Length && format[i + run] == c)
            {
                run++;
            }

            var pattern = format.Substring(i, run);
            Field? field = c switch
            {
                'y' => Field.Year,
                'M' => Field.Month,
                'd' => Field.Day,
                'h' or 'H' => Field.Hour,
                'm' => Field.Minute,
                's' => Field.Second,
                't' => Field.AmPm,
                _ => null,
            };
            yield return field is null ? (pattern, null) : (pattern, field);
            i += run;
        }
    }

    private void OnSpin(object? sender, SpinEventArgs e)
    {
        if (_textBox is { IsKeyboardFocusWithin: false })
        {
            _textBox.Focus(NavigationMethod.Pointer);
        }

        if (_textBox is not null && _textBox.Text != Text)
        {
            TryCommit(_textBox.Text);
        }

        Step(e.Direction == SpinDirection.Increase ? 1 : -1);
        e.Handled = true;
    }

    private void OnTextLostFocus(object? sender, RoutedEventArgs e) => TryCommit(_textBox?.Text);

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryCommit(_textBox?.Text);
            e.Handled = true;
        }
    }

    private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.CaretIndexProperty && _textBox is not null)
        {
            var caret = _textBox.CaretIndex;
            for (var i = 0; i < _sections.Count; i++)
            {
                if (caret >= _sections[i].Start && caret <= _sections[i].Start + _sections[i].Length)
                {
                    _current = i;
                    break;
                }
            }
        }
    }

    private void SelectSection()
    {
        if (_textBox is null || _sections.Count == 0)
        {
            return;
        }

        var section = _sections[Math.Clamp(_current, 0, _sections.Count - 1)];
        var current = _current;
        _textBox.SelectionStart = section.Start;
        _textBox.SelectionEnd = section.Start + section.Length;
        _current = current;
    }

    private enum Field
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second,
        AmPm,
    }

    private readonly record struct Section(Field Field, int Start, int Length);
}
