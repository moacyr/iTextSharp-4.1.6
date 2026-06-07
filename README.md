# iTextSharp 4.1.6 (MPL / LGPLv2)

The MPL / LGPLv2 version of the iTextSharp library, kept for posterity and
maintained as a **cross-platform** PDF library.

This fork targets **`netstandard2.0`** and no longer depends on GDI+
(`System.Drawing.Common`, Windows-only). The raster surface was migrated to
[**SkiaSharp**](https://github.com/mono/SkiaSharp), so the library runs on
Windows, Linux and macOS without `PlatformNotSupportedException`.

## Highlights

- **Target framework:** `netstandard2.0` (library), `net10.0` (tests).
- **No GDI+ / Windows dependency:** `Microsoft.Windows.Compatibility`,
  `UseWindowsForms` and the `System.Drawing` reference were removed.
- **Raster via SkiaSharp 3.119.4:** `SKBitmap` / `SKColor` / `SKMatrix` replace
  `System.Drawing.Bitmap` / `Image` / `Imaging.ImageFormat` / `Drawing2D.Matrix`.
- **Unchanged core:** PDF generation and the managed image decoders
  (`PngImage`, `GifImage`, `TiffImage`, `BmpImage`, `Jpeg`, `Jpeg2000`) are
  untouched. Loading images from `byte[]` / stream / file / URL / base64 works
  exactly as before.
- **`System.Drawing.Color` / `System.Drawing.Point` kept:** they come from
  `System.Drawing.Primitives`, which is part of the `netstandard2.0` contract and
  is cross-platform.

## Dependencies

| Package | Version | Notes |
| --- | --- | --- |
| `SkiaSharp` | 3.119.4 | Cross-platform 2D graphics. |
| `SkiaSharp.NativeAssets.*` | 3.119.4 | Native binaries. Win32 and macOS ship in the metapackage. |

### Cross-platform native assets

The `SkiaSharp` metapackage bundles native binaries for **Windows** and
**macOS** only. To run on **Linux** (containers, CI, servers) add the Linux
native assets to your consuming app:

```xml
<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.119.4" />
```

(The test project already references this so the suite runs on Linux CI.)

## Build & test

```bash
dotnet build itextsharp.sln -c Debug
dotnet test  UnitTests/UnitTests.csproj -c Debug
```

The library builds as `netstandard2.0`; the test project runs on `net10.0`
(MSTest + FluentAssertions).

## Breaking changes (SkiaSharp migration)

Only consumers of the former GDI+ convenience APIs are affected. Code that uses
`byte[]` / streams and normal PDF generation is **not** affected.

| Before (GDI+) | After (SkiaSharp) |
| --- | --- |
| `Image.GetInstance(System.Drawing.Image, ImageFormat)` | `Image.GetInstance(SKBitmap)` — encodes **PNG** (lossless) |
| `Image.GetInstance(System.Drawing.Image, Color[, bool])` | `Image.GetInstance(SKBitmap, Color[, bool])` |
| `Barcode.CreateDrawingImage(...)` → `System.Drawing.Image` | `Barcode.CreateDrawingImage(...)` → `SKBitmap` (params stay `System.Drawing.Color`) |
| `PdfContentByte.Transform(System.Drawing.Drawing2D.Matrix)` | `PdfContentByte.Transform(SKMatrix)` |

## Migration documents

- Design spec: [`docs/superpowers/specs/2026-06-07-itextsharp-skiasharp-net10-migration-design.md`](docs/superpowers/specs/2026-06-07-itextsharp-skiasharp-net10-migration-design.md)
- Implementation plan: [`docs/superpowers/plans/2026-06-07-itextsharp-skiasharp-net10-migration.md`](docs/superpowers/plans/2026-06-07-itextsharp-skiasharp-net10-migration.md)

## License

MPL / LGPLv2 — same license as the original iTextSharp 4.1.6 release.
