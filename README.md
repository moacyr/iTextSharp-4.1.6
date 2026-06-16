# iTextSharp 4.1.6 (MPL / LGPLv2)

The MPL / LGPLv2 version of the iTextSharp library, kept for posterity and
maintained as a **cross-platform** PDF library.

This fork targets **`netstandard2.0`** and no longer depends on GDI+
(`System.Drawing.Common`, Windows-only). It also has **no external graphics
dependency at all**: the small raster surface that used to need GDI+ (and, in an
intermediate step, SkiaSharp) was reimplemented in pure managed C#. The library
runs on Windows, Linux and macOS with **no native binaries** and nothing to keep
updated.

## Highlights

- **Target framework:** `netstandard2.0` (library), `net10.0` (tests).
- **No third-party runtime dependency:** the library references only the
  `netstandard2.0` contract. No GDI+, no SkiaSharp, no native assets.
- **Pure-managed raster:** the barcode `CreateDrawingImage` helpers and
  `Image.GetInstance` use [`DrawingImage`](src/iTextSharp/text/pdf/DrawingImage.cs),
  a tiny 32-bit ARGB raster, and
  [`GraphicsMatrix`](src/iTextSharp/text/pdf/GraphicsMatrix.cs) for transforms.
- **Unchanged core:** PDF generation and the managed image decoders
  (`PngImage`, `GifImage`, `TiffImage`, `BmpImage`, `Jpeg`, `Jpeg2000`) are
  untouched. Loading images from `byte[]` / stream / file / URL / base64 works
  exactly as before.
- **`System.Drawing.Color` / `System.Drawing.Point` kept:** they come from
  `System.Drawing.Primitives`, which is part of the `netstandard2.0` contract and
  is cross-platform.

## Dependencies

The compiled library has **no third-party runtime dependencies** beyond the
`netstandard2.0` reference assemblies. Because there are no native binaries, no
per-platform packages (e.g. `SkiaSharp.NativeAssets.*`) are required — it runs
anywhere a `netstandard2.0` consumer runs.

The **test project** additionally uses MSTest, FluentAssertions and
coverlet (dev-only, not part of the shipped library).

## Build & test

```bash
dotnet build itextsharp.sln -c Debug
dotnet test  UnitTests/UnitTests.csproj -c Debug
```

The library builds as `netstandard2.0`; the test project runs on `net10.0`
(MSTest + FluentAssertions).

## Working with the raster API

`DrawingImage` is a dependency-free 32-bit ARGB raster. Pixels are `0xAARRGGBB`;
pack a `System.Drawing.Color` with its built-in `ToArgb()`:

```csharp
var raster = barcode.CreateDrawingImage(
    System.Drawing.Color.Black, System.Drawing.Color.White);

// raster.Width / raster.Height / raster.GetPixel(x, y) / raster.SetPixel(x, y, argb)
Image pdfImage = Image.GetInstance(raster);   // builds the iText image directly
```

## Breaking changes

Only consumers of the former GDI+ convenience APIs are affected. Code that uses
`byte[]` / streams and normal PDF generation is **not** affected.

| Before (original GDI+ API) | After (pure managed) |
| --- | --- |
| `Image.GetInstance(System.Drawing.Image, ImageFormat)` | `Image.GetInstance(DrawingImage)` |
| `Image.GetInstance(System.Drawing.Image, Color[, bool])` | `Image.GetInstance(DrawingImage, Color[, bool])` |
| `Barcode.CreateDrawingImage(...)` → `System.Drawing.Image` | `Barcode.CreateDrawingImage(...)` → `DrawingImage` (params stay `System.Drawing.Color`) |
| `PdfContentByte.Transform(System.Drawing.Drawing2D.Matrix)` | `PdfContentByte.Transform(GraphicsMatrix)` |

> An intermediate revision used SkiaSharp (`SKBitmap` / `SKColor` / `SKMatrix`)
> for these APIs. That dependency has since been removed in favor of the managed
> `DrawingImage` / `GraphicsMatrix` types above.

## Migration documents

- Remove SkiaSharp (current): [design](docs/superpowers/specs/2026-06-16-itextsharp-remove-skiasharp-dependency-design.md) ·
  [plan](docs/superpowers/plans/2026-06-16-itextsharp-remove-skiasharp-dependency.md)
- GDI+ → SkiaSharp / .NET 10 (previous step): [design](docs/superpowers/specs/2026-06-07-itextsharp-skiasharp-net10-migration-design.md) ·
  [plan](docs/superpowers/plans/2026-06-07-itextsharp-skiasharp-net10-migration.md)

## License

MPL / LGPLv2 — same license as the original iTextSharp 4.1.6 release.
