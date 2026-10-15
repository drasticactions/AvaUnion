# Third-party notices

AvaUnion is licensed under the GNU Lesser General Public License, version 2.1
only (see `LICENSE`). It contains material from the projects below. Each
section says exactly what was taken and its license.

## 1. KDE Union (LGPL-2.1-only OR LGPL-3.0-only OR LicenseRef-KDE-Accepted-LGPL)

© 2020–2026 Arjen Hiemstra and the Union contributors. Version 6.8.0
(`v6.8.0`, `7bb4f3e`), https://invent.kde.org/plasma/union.

Taken, translated from C++ to C# and used under LGPL-2.1-only:

- selector matching (`src/Selector.cpp`: `Selector::matches`,
  `SelectorList::matches`, the selector weights);
- the cascade (`src/Style.cpp`: `Style::matches`; `src/ElementQuery.cpp`:
  `ElementQuery::execute`) and the first-wins merge of
  `StylePropertyGroup::resolveProperties`;
- the color operations (`src/Color.cpp`: Add, Subtract, Multiply, Set, Mix);
- the KColorScheme provider's argument rules
  (`src/platform/kcolorschemeprovider/KColorSchemeProvider.cpp`);
- the Qt Quick positioner layout (`src/output/qtquick/plugin/positioner/*.h`);
- the drawing order and geometry of `StyledRectangle`
  (`src/output/qtquick/plugin/StyledRectangle.cpp` and its scene graph nodes).

`external/union` is included as a git submodule of Union at the pinned tag. The
code in `src/AvaUnion.Theme/Engine/*.g.cs` and `native/src/*.g.*` is derived
from its property schema (`tools/propertygenerator/properties.yml`). The shim
(`native/`) links Union and compiles against its source tree.

`tests/Data/positioner/*.json` are Union's positioner fixtures
(`autotests/output/quick/TestPositionerLayoutData`), BSD-2-Clause per Union's
`REUSE.toml`.

## 2. The Breeze Union style (BSD-2-Clause)

© 2024–2026 Arjen Hiemstra. Part of Union (`src/input/css/styles/breeze*`).

`src/AvaUnion.Theme/Snapshot/breeze.union.bin` and its JSON twin in
`tests/Data/snapshot/` hold rules derived from the `breeze`, `breeze-rtl` and
`breeze-mobile` styles, exported from the parsed style by `union-export`. The
three mask images (`checkmark.svg`, `indeterminate.svg`, `radio.svg`) are kept
as shape primitives.

```
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

## 3. The Breeze color schemes (LGPL-2.0-or-later, used under 2.1)

© The Breeze authors (`colors/BreezeLight.colors`, `colors/BreezeDark.colors`,
https://invent.kde.org/plasma/breeze).

The snapshot holds the Breeze Light and Breeze Dark schemes resolved through
KColorScheme, such as its group, set and role, and the inactive and disabled
effects). `tests/Data/snapshot/BreezeLight.json` and `BreezeDark.json` are the
readable form.

## 4. Avalonia Fluent theme (MIT)

© .NET Foundation and Contributors, https://github.com/AvaloniaUI/Avalonia.

The template structure of the ControlThemes (the named parts each control
looks up, and the popup, scroll viewer, page and window decoration
arrangements) follows Avalonia 12.1.4's Fluent ControlThemes. The managed file
chooser's strings are Fluent's invariant strings.

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 5. Avalonia DataGrid Fluent theme (MIT)

© .NET Foundation and Contributors,
https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid.

The template structure of the ControlThemes in `src/AvaUnion.DataGrid` (the
named parts of the grid, rows, cells and headers) follows the DataGrid's
12.1.2 Fluent theme, under the MIT license quoted in section 4.

## 6. Noto fonts (OFL-1.1)

The browser gallery (`samples/AvaUnion.Gallery.Browser/Assets/Fonts`) bundles
fonts, since a browser has no system fonts.

- Noto Sans (Regular, Bold, Italic, Bold Italic, version 2.015) and Noto Sans
  Mono (Regular, Bold, version 2.014), © 2022 The Noto Project Authors,
  https://github.com/notofonts/latin-greek-cyrillic. License:
  `Assets/Fonts/OFL-NotoSans.txt`.
- Noto Sans CJK JP (Regular, version 2.004), © 2014–2021 Adobe, with Reserved
  Font Name "Source", https://github.com/notofonts/noto-cjk. License:
  `Assets/Fonts/OFL-NotoSansCJK.txt`.

Both are licensed under the SIL Open Font License, version 1.1.
