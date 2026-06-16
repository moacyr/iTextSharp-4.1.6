# Remoção da dependência SkiaSharp — Plano de Implementação

> **Para executores:** usar `superpowers:subagent-driven-development` ou
> `superpowers:executing-plans`. Passos usam checkbox (`- [ ]`).

**Goal:** Remover o pacote NuGet SkiaSharp (e native assets) reimplementando a
superfície usada (`SKBitmap`/`SKColor`/`SKMatrix`) em C# puro.

**Architecture:** Dois tipos gerenciados novos em `iTextSharp.text.pdf`
(`DrawingImage`, `GraphicsMatrix`) substituem os tipos `SK*`. `GetInstance` a
partir do raster passa a montar a imagem direto dos pixels (sem encoder PNG).

**Tech Stack:** C# / netstandard2.0 (lib), net10.0 (testes), MSTest + FluentAssertions.

**Spec:** [`docs/superpowers/specs/2026-06-16-itextsharp-remove-skiasharp-dependency-design.md`](../specs/2026-06-16-itextsharp-remove-skiasharp-dependency-design.md)

---

## Task 1: Criar os tipos gerenciados (`DrawingImage`, `GraphicsMatrix`)

**Files:**
- Create: `src/iTextSharp/text/pdf/DrawingImage.cs`
- Create: `src/iTextSharp/text/pdf/GraphicsMatrix.cs`

- [ ] **Step 1: Criar `DrawingImage.cs`**

```csharp
using System;

namespace iTextSharp.text.pdf {
    /// <summary>
    /// A minimal, dependency-free 32-bit ARGB raster image. Replaces the
    /// SkiaSharp <c>SKBitmap</c> previously returned by the barcode
    /// <c>CreateDrawingImage</c> helpers, so the library no longer needs a
    /// native graphics dependency. Pixels are stored as 0xAARRGGBB, row-major.
    /// </summary>
    public class DrawingImage {
        private readonly int width;
        private readonly int height;
        private readonly int[] pixels;

        public DrawingImage(int width, int height) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");
            this.width = width;
            this.height = height;
            this.pixels = new int[width * height];
        }

        public int Width {
            get { return width; }
        }

        public int Height {
            get { return height; }
        }

        /// <summary>Sets the pixel at (x, y). <paramref name="argb"/> is 0xAARRGGBB.</summary>
        public void SetPixel(int x, int y, int argb) {
            pixels[y * width + x] = argb;
        }

        /// <summary>Gets the pixel at (x, y) as 0xAARRGGBB.</summary>
        public int GetPixel(int x, int y) {
            return pixels[y * width + x];
        }
    }
}
```

- [ ] **Step 2: Criar `GraphicsMatrix.cs`**

```csharp
namespace iTextSharp.text.pdf {
    /// <summary>
    /// A minimal affine transform (PDF graphics matrix) used by
    /// <see cref="PdfContentByte.Transform"/>. Replaces the SkiaSharp
    /// <c>SKMatrix</c>. The six values map to the PDF "cm" operator [a b c d e f].
    /// </summary>
    public struct GraphicsMatrix {
        public float ScaleX;   // a
        public float SkewY;    // b
        public float SkewX;    // c
        public float ScaleY;   // d
        public float TransX;   // e
        public float TransY;   // f

        public GraphicsMatrix(float scaleX, float skewY, float skewX,
                              float scaleY, float transX, float transY) {
            ScaleX = scaleX;
            SkewY = skewY;
            SkewX = skewX;
            ScaleY = scaleY;
            TransX = transX;
            TransY = transY;
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/iTextSharp/text/pdf/DrawingImage.cs src/iTextSharp/text/pdf/GraphicsMatrix.cs
git commit -m "feat: tipos gerenciados DrawingImage e GraphicsMatrix (substituem SK*)"
```

---

## Task 2: `PdfContentByte.Transform` → `GraphicsMatrix`

**Files:**
- Modify: `src/iTextSharp/text/pdf/PdfContentByte.cs` (linha ~8 e ~2848)

- [ ] **Step 1:** Remover `using SkiaSharp;` (linha 8).
- [ ] **Step 2:** Trocar a assinatura/corpo de `Transform`:

```csharp
        public void Transform(GraphicsMatrix tx) {
            ConcatCTM(tx.ScaleX, tx.SkewY, tx.SkewX, tx.ScaleY, tx.TransX, tx.TransY);
        }
```

- [ ] **Step 3: Commit**

```bash
git commit -am "refactor: PdfContentByte.Transform usa GraphicsMatrix"
```

---

## Task 3: `Image.GetInstance` → `DrawingImage` (sem encoder PNG)

**Files:**
- Modify: `src/iTextSharp/text/Image.cs` (using na linha 7; bloco ~544-764)

- [ ] **Step 1:** Remover `using SkiaSharp;` (linha 7).

- [ ] **Step 2:** Substituir o overload com encoder e remover o helper `ToArgb(SKColor)`:

```csharp
        /// <summary>
        /// Converts a DrawingImage raster to an iText Image.
        /// </summary>
        /// <param name="image">the raster to convert</param>
        /// <returns>an iText Image</returns>
        public static Image GetInstance(DrawingImage image) {
            return GetInstance(image, null, false);
        }
```

(Apagar inteiramente o `private static int ToArgb(SKColor c) { ... }`.)

- [ ] **Step 3:** No overload `GetInstance(DrawingImage image, Color color, bool forceBW)`:
  - Trocar a assinatura `SKBitmap image` → `DrawingImage image` e `SKBitmap bm = image;` → `DrawingImage bm = image;`
  - Aplicar estas substituições mecânicas em **todo** o corpo do método:
    - `bm.GetPixel(i, j).Alpha`  →  `(bm.GetPixel(i, j) >> 24) & 0xff`
    - `ToArgb(bm.GetPixel(i, j))`  →  `bm.GetPixel(i, j)`
  - (Ex.: `int alpha = (ToArgb(bm.GetPixel(i, j)) >> 24) & 0xff;` vira `int alpha = (bm.GetPixel(i, j) >> 24) & 0xff;`)

- [ ] **Step 4:** No overload `GetInstance(DrawingImage image, Color color)`: trocar `SKBitmap` → `DrawingImage` (corpo inalterado: `return Image.GetInstance(image, color, false);`).

- [ ] **Step 5: Compilar** `dotnet build src/itextsharp.csproj` — esperado: ainda falha nos barcodes (Task 4), mas Image.cs sem erros de `SK*`.

- [ ] **Step 6: Commit**

```bash
git commit -am "refactor: Image.GetInstance usa DrawingImage (construcao direta, sem PNG)"
```

---

## Task 4: Barcodes `CreateDrawingImage` → `DrawingImage`

**Files (10):**
- `src/iTextSharp/text/pdf/Barcode.cs` (abstrato)
- `Barcode39.cs`, `Barcode128.cs`, `BarcodeInter25.cs`, `BarcodeEAN.cs`,
  `BarcodeCodabar.cs`, `BarcodePostnet.cs`, `BarcodePDF417.cs`, `BarcodeDatamatrix.cs`
- `BarcodeEANSUPP.cs` (só tipo de retorno)

- [ ] **Step 1: `Barcode.cs`** — remover `using SkiaSharp;`; assinatura abstrata:

```csharp
        public abstract DrawingImage CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background);
```

- [ ] **Step 2: Cada barcode que desenha** — aplicar a regra mecânica:
  - Remover `using SkiaSharp;`.
  - `public [override|virtual] SKBitmap CreateDrawingImage(...)` → `... DrawingImage CreateDrawingImage(...)`.
  - Substituir as duas linhas de cor:
    ```csharp
    SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
    SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
    ```
    por:
    ```csharp
    int fg = foreground.ToArgb();
    int bg = background.ToArgb();
    ```
  - `SKBitmap bmp = new SKBitmap(W, H);` → `DrawingImage bmp = new DrawingImage(W, H);` (manter as expressões `W`/`H` de cada arquivo).
  - `SKColor c = bg;` → `int c = bg;` (onde existir).
  - `SetPixel(...)` permanece igual (agora recebe `int`).

- [ ] **Step 3: `BarcodeEANSUPP.cs`** — remover `using SkiaSharp;`; trocar só o tipo de retorno para `DrawingImage` (corpo continua `throw new InvalidOperationException(...)`).

- [ ] **Step 4: `BarcodeDatamatrix.cs`** — idem; manter `if (image == null) return null;`.

- [ ] **Step 5: Compilar** `dotnet build src/itextsharp.csproj` — esperado: **0 erros** (lib ainda referencia SkiaSharp no csproj, mas sem usos).

- [ ] **Step 6: Commit**

```bash
git commit -am "refactor: barcodes CreateDrawingImage retornam DrawingImage"
```

---

## Task 5: Remover o pacote SkiaSharp dos csproj

**Files:**
- Modify: `src/itextsharp.csproj`
- Modify: `UnitTests/UnitTests.csproj`

- [ ] **Step 1:** Em `src/itextsharp.csproj`, remover a linha
  `<PackageReference Include="SkiaSharp" Version="3.119.4" />` (e o `<ItemGroup>`
  se ficar vazio).
- [ ] **Step 2:** Em `UnitTests/UnitTests.csproj`, remover
  `<PackageReference Include="SkiaSharp" Version="3.119.4" />` e
  `<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="3.119.4" />`.
- [ ] **Step 3:** `dotnet build itextsharp.sln` — esperado: **0 erros** (a lib não usa mais SkiaSharp; os testes ainda referenciam SkiaSharp → falham na Task 6).
- [ ] **Step 4: Commit**

```bash
git commit -am "build: remove pacote SkiaSharp e native assets dos csproj"
```

---

## Task 6: Reescrever os testes sem SkiaSharp

**Files:**
- Delete: `UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs`
- Create: `UnitTests/iTextSharp/text/GraphicsApiTests.cs`

- [ ] **Step 1:** `git rm UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs`
- [ ] **Step 2:** Criar `GraphicsApiTests.cs`:

```csharp
using System.IO;
using FluentAssertions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.iTextSharp.text
{
    [TestClass]
    public class GraphicsApiTests
    {
        private static DrawingImage SolidImage(int w, int h, System.Drawing.Color color)
        {
            var bmp = new DrawingImage(w, h);
            int argb = color.ToArgb();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    bmp.SetPixel(x, y, argb);
            return bmp;
        }

        [TestMethod]
        public void Image_GetInstance_FromDrawingImage_ReturnsImageWithSameSize()
        {
            DrawingImage bmp = SolidImage(10, 12, System.Drawing.Color.Red);

            Image img = Image.GetInstance(bmp);

            img.Should().NotBeNull();
            img.Width.Should().Be(10f);
            img.Height.Should().Be(12f);
        }

        [TestMethod]
        public void Image_GetInstance_FromDrawingImage_WithColor_ReadsPixels()
        {
            DrawingImage bmp = SolidImage(4, 4, System.Drawing.Color.Blue);

            Image img = Image.GetInstance(bmp, null);

            img.Should().NotBeNull();
            img.Width.Should().Be(4f);
            img.Height.Should().Be(4f);
        }

        [TestMethod]
        public void Barcode39_CreateDrawingImage_ReturnsDrawingImage()
        {
            var barcode = new Barcode39();
            barcode.Code = "ITEXT";
            barcode.BarHeight = 30;

            DrawingImage bmp = barcode.CreateDrawingImage(
                System.Drawing.Color.Black, System.Drawing.Color.White);

            bmp.Should().NotBeNull();
            bmp.Width.Should().BeGreaterThan(0);
            bmp.Height.Should().Be(30);
        }

        [TestMethod]
        public void PdfContentByte_Transform_WritesConcatMatrixOperator()
        {
            using var ms = new MemoryStream();
            var doc = new Document();
            PdfWriter writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();

            PdfContentByte cb = writer.DirectContent;
            cb.Transform(new GraphicsMatrix(2f, 0f, 0f, 3f, 10f, 20f));
            string content = cb.InternalBuffer.ToString();

            doc.Close();

            content.Should().Contain("cm");
        }
    }
}
```

- [ ] **Step 3: Rodar testes** `dotnet test UnitTests/UnitTests.csproj` — esperado: mesma contagem de antes (~18 passed / 1 skipped), **0 failed**.
- [ ] **Step 4: Commit**

```bash
git add UnitTests/iTextSharp/text/GraphicsApiTests.cs
git commit -am "test: GraphicsApiTests sem SkiaSharp"
```

---

## Task 7: Verificação final, README e doc gerado

**Files:**
- Modify: `README.md`
- Modify: `src/iTextSharp.xml` (regenerado pelo build)

- [ ] **Step 1: Varredura** — `grep -rn "SkiaSharp\|SK[A-Z]" src UnitTests` não deve
  achar nada além de `Chunk.SKEW`, `RtfParser.*SKIP*`, `SkipjackEngine`, `CSKSC.../CSKOI8R`.
- [ ] **Step 2: Grafo de dependências** — confirmar que `SkiaSharp` e
  `SkiaSharp.NativeAssets.*` sumiram (`dotnet list package` em ambos os projetos).
- [ ] **Step 3: README.md** — trocar a seção de dependências/native assets por
  "sem dependência externa de gráfico (raster 100% gerenciado)"; atualizar a
  tabela de breaking changes para `DrawingImage`/`GraphicsMatrix`.
- [ ] **Step 4:** Garantir que `src/iTextSharp.xml` regenerado foi commitado
  (preservar encoding/BOM).
- [ ] **Step 5: Commit**

```bash
git commit -am "docs: README e doc gerado refletem a remocao do SkiaSharp"
```

---

## Acceptance (do spec)

1. Solution compila (lib `netstandard2.0`, testes `net10.0`), **0 erros**.
2. Testes: mesma contagem de antes (~18 passed / 1 skipped), **0 failed**.
3. Sem referências a `SkiaSharp`/`SK*` em `src/` e `UnitTests/` (exceto tokens não relacionados).
4. Grafo de dependências sem `SkiaSharp` / native assets.
