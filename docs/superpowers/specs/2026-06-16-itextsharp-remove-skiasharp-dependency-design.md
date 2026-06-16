# Remoção da dependência SkiaSharp (raster 100% gerenciado) — Design

**Data:** 2026-06-16
**Contexto:** continuação da migração GDI+ → SkiaSharp
([spec de 2026-06-07](2026-06-07-itextsharp-skiasharp-net10-migration-design.md)).
A migração anterior trocou o GDI+ pelo SkiaSharp. Este documento remove **também**
o SkiaSharp, deixando a biblioteca sem nenhuma dependência de gráfico.

## Objetivo

Eliminar por completo o pacote NuGet `SkiaSharp` e os `SkiaSharp.NativeAssets.*`,
reimplementando em C# puro a pequena superfície que a biblioteca de fato usa.
Resultado: **zero dependências de gráfico, sem binário nativo, multiplataforma,
nada para atualizar**. Mantém `netstandard2.0` (lib) e `net10.0` (testes).

## Por que não "vendorizar" o SkiaSharp

SkiaSharp = bindings gerenciados (MIT) **+** binários nativos C++ (`libSkiaSharp`,
do projeto Skia do Google). Copiar só a fonte gerenciada **não** remove a
necessidade dos nativos — e atualizar os nativos por causa de CVEs é exatamente o
que se quer evitar. Como o uso real é minúsculo, **reimplementar** a superfície é
mais simples e elimina a dependência de verdade.

## Superfície usada (inventário completo)

| Tipo | API usada |
| --- | --- |
| `SKBitmap` | `new(w,h)`, `Width`, `Height`, `SetPixel(x,y,SKColor)`, `GetPixel(x,y)`, `Encode(Png,100)` |
| `SKColor` | `new(r,g,b,a)`, `.Red` / `.Green` / `.Blue` / `.Alpha` |
| `SKMatrix` | 6 campos (`ScaleX, SkewY, SkewX, ScaleY, TransX, TransY`) em `PdfContentByte.Transform` |
| `SKEncodedImageFormat` / `SKData` | `.Png` / `.ToArray()` (só no overload `GetInstance(SKBitmap)`) |

Consumidores na `src/`: `Image.cs` (3 overloads), `Barcode.cs` (abstrato) + 9
barcodes, `PdfContentByte.cs` (`Transform`).

## Tipos novos (em `src/iTextSharp/text/pdf/`)

### `DrawingImage` — substitui `SKBitmap`

Raster ARGB de 32 bits, sem dependências. Pixels em `int[]` no formato
`0xAARRGGBB`, row-major.

```csharp
public class DrawingImage {
    public DrawingImage(int width, int height);
    public int Width  { get; }
    public int Height { get; }
    public void SetPixel(int x, int y, int argb);   // 0xAARRGGBB
    public int  GetPixel(int x, int y);             // 0xAARRGGBB
}
```

As cores são empacotadas com `System.Drawing.Color.ToArgb()`, que já devolve
`0xAARRGGBB` — não é preciso um helper de cor. Não é `IDisposable` (some o
problema de descartar `SKBitmap`).

### `GraphicsMatrix` — substitui `SKMatrix`

`struct` com os seis valores do operador PDF `cm` (`[a b c d e f]`):

```csharp
public struct GraphicsMatrix {
    public float ScaleX, SkewY, SkewX, ScaleY, TransX, TransY;   // a b c d e f
    public GraphicsMatrix(float scaleX, float skewY, float skewX,
                          float scaleY, float transX, float transY);
}
```

## Mudanças por arquivo

- **`Image.cs`** — 3 overloads `SKBitmap` → `DrawingImage`. `GetInstance(DrawingImage)`
  passa a delegar para `GetInstance(image, null, false)` (**construção direta dos
  pixels**, sem encoder PNG). Remove `using SkiaSharp;` e o helper privado
  `ToArgb(SKColor)`; como `GetPixel` já devolve `int` ARGB,
  `ToArgb(bm.GetPixel(i,j))` vira `bm.GetPixel(i,j)` e
  `bm.GetPixel(i,j).Alpha` vira `(bm.GetPixel(i,j) >> 24) & 0xff`.
- **`Barcode.cs`** — método abstrato passa a retornar `DrawingImage`. Remove `using SkiaSharp;`.
- **8 barcodes que desenham** (`Barcode39`, `Barcode128`, `BarcodeInter25`,
  `BarcodeEAN`, `BarcodeCodabar`, `BarcodePostnet`, `BarcodePDF417`,
  `BarcodeDatamatrix`) — retorno `DrawingImage`; `new DrawingImage(w,h)`;
  `int fg = foreground.ToArgb(); int bg = background.ToArgb();`; `SetPixel` com
  `int`. Remove `using SkiaSharp;`.
- **`BarcodeEANSUPP.cs`** — só muda o tipo de retorno (corpo continua `throw`). Remove `using SkiaSharp;`.
- **`PdfContentByte.cs`** — `Transform(GraphicsMatrix tx)` chamando
  `ConcatCTM(tx.ScaleX, tx.SkewY, tx.SkewX, tx.ScaleY, tx.TransX, tx.TransY)`.
  Remove `using SkiaSharp;`.
- **`src/itextsharp.csproj`** — remove o `PackageReference` do `SkiaSharp`.
- **`UnitTests/UnitTests.csproj`** — remove `SkiaSharp` e `SkiaSharp.NativeAssets.Linux`.
- **Testes** — `SkiaSharpMigrationTests.cs` → `GraphicsApiTests.cs`, reescrito sem SkiaSharp.
- **`README.md`** — atualizar para "sem dependência externa de gráfico".
- **`src/iTextSharp.xml`** — regenerar (doc gerado no build).

## Comportamento / breaking changes

Os tipos públicos `SK*` introduzidos em 2026-06-07 (ainda não publicados em
release) voltam a tipos próprios:

| Antes (SkiaSharp) | Depois (gerenciado) |
| --- | --- |
| `Image.GetInstance(SKBitmap[, Color[, bool]])` | `Image.GetInstance(DrawingImage[, Color[, bool]])` |
| `Barcode.CreateDrawingImage(...) → SKBitmap` | `Barcode.CreateDrawingImage(...) → DrawingImage` |
| `PdfContentByte.Transform(SKMatrix)` | `PdfContentByte.Transform(GraphicsMatrix)` |

`GetInstance(DrawingImage)` agora monta a imagem direto dos pixels (RGB + máscara
alfa) em vez de fazer round-trip por PNG — **visualmente idêntico** para barcodes
e imagens opacas.

## Critérios de aceite

1. A solution compila (lib `netstandard2.0`, testes `net10.0`), **0 erros**.
2. Os testes passam com a mesma contagem de antes (~18 passed / 1 skipped).
3. Nenhuma referência a `SkiaSharp`/`SK*` em `src/` e `UnitTests/` (exceto os
   tokens não relacionados `Chunk.SKEW`, `RtfParser.*_SKIP_*`, `SkipjackEngine`).
4. O grafo de dependências não contém `SkiaSharp` nem `SkiaSharp.NativeAssets.*`.
