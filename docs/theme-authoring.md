# Writing a ControlTheme

A ControlTheme in AvaUnion uses Union for drawing its layouts.

## Naming elements

```xml
<Setter Property="union:Union.Type" Value="button" />
```

marks the control as a Union element. Template parts that are elements of
their own (an indicator, a handle) set `union:Union.Type` on the part. Parts
without a type are not elements; they see the style of the nearest element
above them.

| Attached property | Use |
|---|---|
| `Union.Type` | The lowercase Union type. An empty string makes an untyped element, such as the text field placeholder. |
| `Union.Hints` | Extra hints, space-separated. Classes are hints too. |
| `Union.Attributes` | `name=value;name=value`. |
| `Union.ColorSet` | Shorthand for the `colorset` attribute. |
| `Union.StateSource` | `Auto` (the element for templated controls, the templated parent for other parts), `Self`, `TemplatedParent` or `None`. |
| `Union.StyleId` | The style package for a subtree; `UnionScope` sets it. |

## Reading the style

`{union:Style Path}` binds to a value of the nearest element at or above the
target. The path is a dotted `ResolvedStyle` path; `StylePaths.All` lists
them and a repository test checks every path used in the themes.

```xml
<Setter Property="Padding" Value="{union:Style Layout.Padding}" />
<Setter Property="Foreground" Value="{union:Style Text.Color}" />
<union:UnionBox Margin="{union:Style Layout.Inset}" />
<Border Padding="{union:Style Layout.Spacing, Sides=Right}" />
<union:UnionIcon IsVisible="{union:Style Display.Visible, Fallback=False}" />
```

Values convert to the target type: colors to brushes, size groups to
`Thickness`, corners to `CornerRadius`, wrap modes to `TextWrapping`, elide
modes to `TextTrimming`, fonts to sizes, weights and styles. An unset leaf
gives the `Fallback`. A color bound to a brush property is a `SolidColorBrush` that
AvaUnion recolors in place when the scheme changes, shared by elements with
the same style: read it, but don't change it.

`{union:SchemeColor View, Foreground, Link}` reads one KColorScheme color, for
the few roles Union's CSS does not cover. Resource keys follow the pattern
`Union.{Group}.{Set}.{Kind}.{Role}` (a `Color`) and `…Brush`.

## The skeleton

Button-like controls follow one shape:

```xml
<ControlTemplate>
  <Panel>
    <union:UnionBox Margin="{union:Style Layout.Inset}" />
    <union:UnionPositioner>
      <union:UnionIcon union:PositionedItem.Source="Icon" />
      <ContentPresenter Name="PART_ContentPresenter" union:PositionedItem.Source="Text" … />
      <union:UnionIcon union:Union.Type="indicator" union:PositionedItem.Source="Layout"
                       IsVisible="{union:Style Display.Visible, Fallback=False}" />
    </union:UnionPositioner>
  </Panel>
</ControlTemplate>
```

`UnionBox` draws the element's background, border, outline and shadow.
`UnionPositioner` places its children: `Source="Icon"` and `Source="Text"`
use the owner's `icon.alignment` and `text.alignment`; `Source="Layout"` uses
the child's own `layout.alignment` and `layout.margins`. `UnionBox
ImplicitSize="True"` measures to `layout.width × layout.height`, like a Qt
Quick control's implicit background size.

## Right to left

An element whose `FlowDirection` is `RightToLeft` and that follows the
session's `breeze` style uses `breeze-rtl`, as Union's Plasma platform plugin
does for a right-to-left Qt application. Popups follow the direction of the
control that opens them.

- `UnionBox` and `UnionIcon` are not mirrored, so borders, check marks and
  style icons look as Union draws them.
- `UnionPositioner` does not mirror its own placement. Avalonia does that.
- `{union:Style}` swaps the left and right sides of a `Thickness` or
  `CornerRadius`, so that a Union `padding-left` stays on the left. A binding
  with `Sides` is not swapped, because the template chose that side.
- Text alignments become `Start` and `End`.

An icon that the template chooses and that points in the reading direction,
like the next and previous buttons of a calendar, sets
`MirrorInRightToLeft="True"`.

## Element mapping

The stock controls map to Union's Qt Quick vocabulary. `›` is a template
part; hints in parentheses are added automatically when they apply.

| Avalonia | Chain |
|---|---|
| Window, EmbeddableControlRoot | `applicationwindow` (`inactive`, `maximized`) |
| PopupRoot, OverlayPopupHost | untyped; chains continue at the window (`menu` for menu popups) |
| WindowDrawnDecorations | each layer is a `titlebar` › `minimizebutton`, `maximizebutton`, `closebutton` (`maximized`) |
| Button, RepeatButton, ToggleButton | `button` › `indicator` (`with-menu`, `with-icon`, `flat`, `changed`, `[display]`) |
| HyperlinkButton | `button.flat`, text from `View/Foreground/Link` |
| SplitButton, ToggleSplitButton, DropDownButton | `button.with-menu-button` / `button.with-menu` › `indicator` |
| CheckBox, RadioButton | `checkbox`, `radiobutton` › `indicator` (`[display]`) |
| ToggleSwitch | `switch` › `indicator` › `handle` |
| TextBox | `textfield` (or `textarea`) › untyped `.placeholder` |
| AutoCompleteBox | its TextBox (`textfield`); suggestions in `popup.combobox` › `itemdelegate.inside-list` |
| NumericUpDown, ButtonSpinner | `spinbox` › `indicator` (`editable`, `constrained`) |
| ComboBox, ComboBoxItem | `combobox` › `indicator`; list in `popup.combobox` › `itemdelegate.inside-list` |
| ListBox, ListBoxItem | `scrollview.framed-background[colorset=view]` › `itemdelegate.inside-list.hover-enabled` (`last-in-list`) |
| ItemsControl | untyped |
| TreeView, TreeViewItem | `scrollview…` › `treeviewdelegate` › `indicator` (`expanded`, `selected`) |
| TableView, rows, cells, column headers | `scrollview` › `tableviewdelegate` (`alternating-colors` on odd rows unless the table has the class `no-alternating-colors`); `headerview.horizontal` › `headerviewdelegate.horizontal` |
| ScrollViewer, ScrollBar | `scrollview` › `scrollareacorner`; `scrollbar.vertical`/`.horizontal` › `handle` |
| Slider | `slider.horizontal` › `fill`, `handle`, `tickmark` (`active` at or below the value) |
| ProgressBar | `progressbar.horizontal` › `track` (`indeterminate`) |
| Menu, MenuItem | `menubar` › `menubaritem`; `menu` › `menuitem` › `indicator`, `shortcuttext`, `arrow` |
| ContextMenu, MenuFlyoutPresenter, FlyoutPresenter | `menu`, `popup` (the popup's element) |
| Separator | `toolseparator` (`horizontal`/`vertical`), or `menuseparator` in a menu |
| ToolTip | `tooltip` (the popup's element) |
| Label | `label` |
| TabControl, TabItem | `tabwidget` › `tabbar` › `tabbutton` (`selected` → `:checked`, `[direction]`) |
| TabStrip, TabStripItem | `tabbar` › `tabbutton` |
| Expander | header `toolbutton` with an arrow icon (`expanded`); content in a `frame` |
| HeaderedContentControl, GroupBox | `groupbox` › `header` |
| SplitView | `pane` / `drawer[edge]`; overlay `overlay.modal` |
| Carousel, TransitioningContentControl | `swipeview` |
| ContentPage, NavigationPage, TabbedPage, DrawerPage, CarouselPage | `page` › `toolbar.header`, `toolbar.footer`, `tabbar`; drawers `overlay.modal` |
| Calendar, CalendarItem | `dayofweekrow` › `label`; `monthgrid` › `label` (`current-month`, `today`, `selected`) |
| DatePicker, TimePicker, CalendarDatePicker | `combobox`; flyout `popup.combobox` › `tumbler` › `text` |
| CommandBar | `toolbar` › `toolbutton[display]`, `toolseparator.vertical`; overflow in `menu` |
| NotificationCard | `inlinemessage[type][position=inline]` |
| WindowNotificationManager | untyped; each card is an `inlinemessage` |
| PipsPager | `pageindicator` › `pageindicatordelegate` (`current`) |
| GridSplitter | `splitview.horizontal`/`.vertical` › `handle` |
| RefreshContainer, RefreshVisualizer | untyped; the visualizer is a `busyindicator` |
| TextSelectionHandle | `selectionrectangle` › `selectionrectanglehandle` |
| DataValidationErrors | untyped; the error text uses `View/Foreground/Negative` |
| ManagedFileChooser | composed of the themes above: a places `ListBox`, a `headerview`, a framed file list |
| SelectableTextBlock, PathIcon, ThemeVariantScope, AdornerLayer | untyped |

`AvaUnion.DataGrid` adds:

| Avalonia | Chain |
|---|---|
| DataGrid | `tableview[shape=box][colorset=view]` |
| DataGridRow | `tableviewdelegate` (`selected`, `editing`, `alternating-colors`) |
| DataGridCell | untyped (`selected`, `current`, `editing`) |
| DataGridColumnHeader | `headerviewdelegate.horizontal` (`sort-ascending`, `sort-descending`) › `indicator` |
| DataGridRowHeader | `headerviewdelegate.vertical` (`selected`, `current`, `editing`) |
| DataGridRowGroupHeader | `treeviewdelegate` (`expanded`) › `indicator` as the expander |

`AvaUnion.Controls` adds:

| Avalonia | Chain |
|---|---|
| BusyIndicator | `busyindicator` with its icon turning |
| DelayButton | `delaybutton` › `fill` (`with-icon`, `[display]`) |
| Dial | `dial` › `handle` |
| RangeSlider | `rangeslider.horizontal`/`.vertical` › `fill`, `handle.first`, `handle.second` |
| Tumbler, TumblerItem | `tumbler` › `text` (`:highlighted` for the selected item) |
| MonthGrid, DayOfWeekRow, WeekNumberColumn | `monthgrid`, `dayofweekrow`, `weeknumbercolumn` › `label` (`current-month`, `today`) |
| Heading | `heading.level-N` (`primary`, `secondary`) |
| ActionTextField | `textfield` › `toolbutton[display=icon-only]` for each action |
| TimeEdit, DateEdit, DateTimeEdit | `spinbox.editable.constrained` › `indicator` |
| SwipeDelegate | `swipedelegate.hover-enabled` |
| Dialog | `dialog` › `label.header`, `dialogbuttonbox[position=footer]` › `button[role]`; `overlay.dialog.modal` when modal |
| ColumnTreeView | `scrollview` › `headerview.horizontal` › `headerviewdelegate.horizontal`; `treeviewdelegate` (`expanded`, `selected`, `alternating-colors`) › `indentation`, `indicator` |
| ClosableTabs.IsEnabled | `tabbutton` › `closebutton` |
| TableRowHeaders.IsVisible | `headerviewdelegate.vertical` before each `tableviewdelegate` |

Control libraries describe their own controls the same way through
`UnionHintProviders`: `MapPseudoClass<T>(":sortascending", "sort-ascending")`
makes a pseudo-class a hint instead of a state, and `Register<T>(provider,
watchedProperties)` computes hints and attributes from properties.

`tools/union-dump` prints what the managed engine resolves for a chain:

```sh
dotnet run tools/union-dump/union-dump.cs -- "applicationwindow > button:hovered.flat"
```
