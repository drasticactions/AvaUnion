# App styles

An application can ship its own Union style package and use it for itself.

- The native engine loads the style from the package folder through Union.
- The managed engine loads a snapshot of the same package. You build the
  snapshot with Union, in CI or on your computer, and commit it.

## The package

A package is a folder. The folder name is the style id.

```
MyApp/
  Styles/
    mystyle/
      metadata.json
      contents/css/style.css
      contents/images/*.svg
    mystyle.union.bin          the snapshot built by you, see below.
  MyApp.csproj
```

`contents/css/style.css` is the entry point. It can import other files in
`contents/css` with `@import "file.css";`.

`metadata.json` has these keys:

| Key | Value |
|---|---|
| `input-type` | `css`. This key is necessary. |
| `name` | The display name. `name[de_DE]` gives the name for one locale. |
| `description` | A short description. `description[de_DE]` gives it for one locale. |
| `version` | The package version, as text. |
| `license` | An SPDX license id. |
| `url` | The home page. |
| `authors` | An array of names. |
| `hidden` | `true` removes the style from style lists. `GetStyle` still finds it. |

`UnionStyleHandle.Metadata` and `IUnionEngine.Styles` give these values as
`UnionStyleMetadata`. For the name and the description, the current UI
culture selects the localized entry. If there is no entry for the culture,
the entry without a suffix is used.

### A package must be self-contained

A rule must not refer to a file outside the package. This applies to
`@import`, `url()` and image paths. Union resolves an `@import` relative to
the importing file. Breeze-RTL can import Breeze with
`@import "../../../breeze/contents/css/style.css"` because it is installed
next to Breeze. An app package is in `union-styles/` in the app folder, and
Breeze is not next to it. Thus you cannot build on Breeze with `@import`.
Copy the rules that you need into your package.

The snapshot tool stops with an error when a rule refers to a file outside
the package. It also stops when the CSS has parse errors.

### Give each variable a name that only your style uses

Union's CSS parser keeps one list of CSS variables for the whole process.
The first stylesheet that defines a variable sets its value, and the parser
ignores a later definition with the same name. On the native engine, Union
loads the default style of the session before your package. If your package
defines `--highlight-color`, as Breeze does, your rules get the Breeze value.

Put the style id at the start of each variable name:

```css
:root {
    --mystyle-accent: custom-color("kcolorscheme", "active", "selection", "background", "normal");
}

button:highlighted {
    background-color: var(--mystyle-accent);
}
```

The snapshot tool stops with an error when your package defines a variable
that an installed style or Union's `default.css` also defines. The error
names the variable and the styles that define it. This check also applies
when you copy rules from Breeze: rename the variables that you copy.

## Add the package to the app

Add the `UnionStylePackage` item to the project file:

```xml
<ItemGroup>
  <UnionStylePackage Include="Styles/mystyle" Snapshot="Styles/mystyle.union.bin" />
</ItemGroup>
```

| Metadata | Meaning |
|---|---|
| `Include` | The package folder. |
| `Snapshot` | The committed snapshot. Without it, the build gives a warning, and the managed engine does not have the style. |
| `Id` | The style id. The default is the folder name. |

The build does these steps:

- It copies the package to `union-styles/mystyle/` in the output folder and
  in the publish folder.
- It embeds the snapshot as the resource `AvaUnion.Styles.mystyle.union.bin`.
- It stops with an error if `metadata.json` or the snapshot file is missing.

The `AvaUnion.Theme` NuGet package adds the item through `buildTransitive`.

At startup, the engine finds the packages of the entry assembly. It finds a
package from its embedded snapshot, from its folder in `union-styles/`, or
from both. Set `AvaUnionOptions.DiscoverStylePackages` to `false` to stop
this.

### A style in a library

Discovery reads the entry assembly only. If a library ships the style, add
the library's packages by hand:

```csharp
var options = new AvaUnionOptions();
foreach (var package in UnionStylePackage.Discover(typeof(MyLibraryType).Assembly))
{
    options.StylePackages.Add(package);
}

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithAvaUnion(options)
    .StartWithClassicDesktopLifetime(args);
```

The `UnionStylePackage` item goes in the library project. The package folders
go to the output folder of each app that references the library.

`UnionStylePackage.FromDirectory` and `UnionStylePackage.FromSnapshot` make a
package from a folder or from a stream. Add them to
`AvaUnionOptions.StylePackages`.

The engine reads the list one time, when it starts. You cannot add packages
after that.

## Use the style

Set the style id on the theme or on a scope:

```xml
<union:UnionTheme StyleId="mystyle" />
```

## Build the snapshot

You need Union, Qt 6 and the KDE Frameworks from your distribution, and a
clone of this repository at the same tag as your app's `AvaUnion.Theme`:

```sh
tools/build-style-snapshot.sh path/to/Styles/mystyle path/to/Styles/mystyle.union.bin
tools/build-style-snapshot.sh path/to/Styles/mystyle path/to/Styles/mystyle.union.bin --check
```

The script builds the shim in `native/build` if it is not there. With
`--check`, it writes nothing and exits with 1 if the file is stale.

## How the engines use the package

An app package has priority over an installed style with the same id. This
is also true when that id is the default style of the session.

| | Native engine | Managed engine |
|---|---|---|
| Source | The package folder | The embedded snapshot |
| CSS errors | Union uses the rules that parsed. | Not possible. The snapshot tool refuses CSS with errors. |
| Default style | The app package replaces the session default when the ids are the same. | The default is `breeze`. An app package with the id `breeze` replaces it. |
| Colors | The session color scheme, or `UnionTheme.ColorScheme` | Breeze Light or Breeze Dark |

### Log lines

If an engine cannot use a package, it logs a warning for each id and then
uses the style that it would use for an unknown id. The log area is
`AvaUnion`.

| Line | Cause |
|---|---|
| `AvaUnion: style <id> unavailable: no snapshot for the managed engine` | The package has no snapshot. |
| `AvaUnion: style <id> unavailable: no package folder for the native engine` | The package has no folder. |
| `AvaUnion: style <id> unavailable: <reason>` | The snapshot is damaged or has a different property schema, or Union refused the folder. |
| `AvaUnion: style <id> has CSS errors` | Union found parse errors. The native engine uses the style. |

The native engine cannot detect a variable that another style defined
first. Such a style loads without a log line, but it shows the other
style's value. Build the snapshot to find these variables.

When no style with the id exists after this, the theme logs
`Style <id> not found; using the default style`.

## Examples

- `tests/Data/styles/avaunion-test` is a small package for the tests. The
  tests compare both engines with Union's results for it.
- `samples/AvaUnion.Gallery/Styles/avaunion-demo` is the demo style of the
  gallery. The gallery adds it with `UnionStylePackage.Discover` because the
  style ships in the shared gallery library.
