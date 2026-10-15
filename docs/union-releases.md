# Following a Union release

The shim compiles against Union's property headers, whose classes are
generated from Union's `properties.yml`. Each shim is built for one Union
version (`libavaunion-native-<version>.so`), but it loads on every Union
with the same property schema.

`tools/union-schemas` writes the SHA-256 of `properties.yml` at every release
tag of `external/union`, up to the pinned version, to `UnionSchemas.g.cs` and
`UnionSchemas.g.h`. CI fetches the tags and checks that the table is up to
date. A shim
loads on the installed Union when one of these is true:

- the installed version has the schema the shim was generated from;
- the installed version is not in the table, its patch number is below 80,
  and the newest release in the table with the same major.minor has that
  schema;
- the installed version is the version the shim was built for.

`NativeLibraryResolver.Compatible` chooses the shim, and `avu_probe` checks
the same rule in the shim. Anything else falls back to the managed engine.

The package ships one shim for each architecture, built for the pinned Union
version. The "Native shim" workflow (`.github/workflows/native.yml`) builds
them when the native sources or the Union submodule change. The CI pack job
downloads them with `tools/natives/fetch-natives.sh`. Packing fails when a shim for the pinned
version is missing.

## The Union submodule

`external/union` is Union's source tree at the pinned release. CI builds and
installs Union from it with `tools/natives/build-union.sh`, so a new Union
release does not have to be in a distribution's repositories first. The shim
compiles against it, and `tools/union-properties` reads its `properties.yml`.

The pinned version is the `PROJECT_VERSION` in `external/union/CMakeLists.txt`.
The shim names, the Union tag in the generated code and `AvaUnionUnionVersion`
in MSBuild all come from it.

## Package versions

A package version is the pinned Union version plus a fourth number from the 
CI build, for example `6.8.0.152`.
A repository test keeps the snapshot at the same version.

## Moving to a new tag

1. Move the submodule to the new tag:

   ```sh
   git -C external/union fetch --tags
   git -C external/union checkout v6.9.0
   ```

2. Build and install that Union:

   ```sh
   tools/natives/build-union.sh
   ```

3. Regenerate both sides of the boundary from the new `properties.yml`, and
   the table of schemas:

   ```sh
   dotnet run tools/union-properties/union-properties.cs
   dotnet run tools/union-schemas/union-schemas.cs
   ```

   The generated header carries the schema hash and the tag; a repository
   test checks it against the submodule.
4. Build the shim and regenerate the data (snapshot, parity corpus, expected
   native results):

   ```sh
   cmake -S native -B native/build -G Ninja
   cmake --build native/build
   tools/regenerate-data.sh
   ```

   CI regenerates the data in an `archlinux:latest` container and fails when
   it differs from the repository. Regenerate it in the same container, so
   that the Breeze color schemes are the same.

   The script also rebuilds the snapshots of the test style package and the
   gallery's demo style, and their export data. When the property schema
   changes, every app style snapshot is stale. Apps must build their
   snapshots again with the new AvaUnion tag. `docs/app-styles.md` tells how.
5. Run the tests. The parity tests compare the managed engine with the new
   Union on the whole corpus. Review the corpus diff, since Breeze can
   change between versions.
6. Update the version in `NOTICE.md`, and commit the submodule with the
   regenerated files.
