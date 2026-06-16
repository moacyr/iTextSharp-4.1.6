# iTextSharp → SkiaSharp (netstandard2.0, sem GDI) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remover a dependência de GDI+ (`System.Drawing.Common`, Windows-only) da biblioteca iTextSharp 4.1.6, substituindo a superfície raster por SkiaSharp, mantendo o alvo `netstandard2.0` e atualizando o projeto de testes para `net10.0`.

**Architecture:** A biblioteca permanece `netstandard2.0`. Os tipos cross-platform `System.Drawing.Color` e `System.Drawing.Point` (de `System.Drawing.Primitives`, já no contrato netstandard2.0) são mantidos. Apenas o GDI+ raster (`System.Drawing.Bitmap`/`Image`/`Imaging.ImageFormat`/`Drawing2D.Matrix`) é trocado por SkiaSharp (`SKBitmap`/`SKColor`/`SKMatrix`). O core de geração de PDF e os decoders de imagem nativos (`PngImage`, `GifImage`, etc.) não mudam.

**Tech Stack:** C#, netstandard2.0 (lib), net10.0 (testes), SkiaSharp 3.119.4, MSTest + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-06-07-itextsharp-skiasharp-net10-migration-design.md`

---

## File Structure

**Modificados (lib):**
- `src/itextsharp.csproj` — remover pacotes/flags GDI/Windows, adicionar SkiaSharp.
- `src/iTextSharp/text/pdf/PdfContentByte.cs` — `Transform(Matrix)` → `Transform(SKMatrix)`.
- `src/iTextSharp/text/Image.cs` — 3 sobrecargas `GetInstance(System.Drawing.Image...)` → `GetInstance(SKBitmap...)` + helper `ToArgb`.
- `src/iTextSharp/text/pdf/Barcode.cs` — método abstrato `CreateDrawingImage` → retorna `SKBitmap`.
- `src/iTextSharp/text/pdf/Barcode39.cs`, `Barcode128.cs`, `BarcodeInter25.cs`, `BarcodeEAN.cs`, `BarcodeEANSUPP.cs`, `BarcodeCodabar.cs`, `BarcodePostnet.cs` — overrides `CreateDrawingImage` → `SKBitmap`.
- `src/iTextSharp/text/pdf/BarcodePDF417.cs`, `BarcodeDatamatrix.cs` — métodos `virtual` `CreateDrawingImage` → `SKBitmap`.

**Modificados (testes):**
- `UnitTests/UnitTests.csproj` — `net6.0` → `net10.0` + SkiaSharp.

**Criados (testes):**
- `UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs` — testes da nova API.

**Importante (sequência):** SkiaSharp e `Microsoft.Windows.Compatibility` coexistem temporariamente. SkiaSharp é adicionado primeiro (Task 1); o pacote Windows só é removido depois que todo o código GDI sumiu (Task 5). Assim o projeto compila após cada task.

---

## Task 1: Adicionar SkiaSharp ao projeto da biblioteca

**Files:**
- Modify: `src/itextsharp.csproj`

- [ ] **Step 1: Adicionar o PackageReference do SkiaSharp**

Edite `src/itextsharp.csproj`. Localize:

```xml
    <PackageReference Include="Microsoft.Windows.Compatibility" Version="7.0.5" />
  </ItemGroup>
```

Substitua por:

```xml
    <PackageReference Include="Microsoft.Windows.Compatibility" Version="7.0.5" />
    <PackageReference Include="SkiaSharp" Version="3.119.4" />
  </ItemGroup>
```

- [ ] **Step 2: Restaurar e compilar para confirmar o SkiaSharp**

Run: `dotnet build src/itextsharp.csproj -c Debug`
Expected: BUILD SUCCEEDED (o SkiaSharp é restaurado; o código antigo ainda usa GDI mas o pacote Windows.Compatibility continua presente).

- [ ] **Step 3: Commit**

```bash
git add src/itextsharp.csproj
git commit -m "build: adiciona SkiaSharp 3.119.4 à biblioteca"
```

---

## Task 2: Migrar PdfContentByte.Transform para SKMatrix

`System.Drawing.Drawing2D.Matrix.Elements` = `{m11, m12, m21, m22, dx, dy}`. O `SKMatrix` equivalente mapeia: `m11→ScaleX`, `m12→SkewY`, `m21→SkewX`, `m22→ScaleY`, `dx→TransX`, `dy→TransY`. A chamada original `ConcatCTM(c[0], c[1], c[2], c[3], c[4], c[5])` preserva-se com `ConcatCTM(ScaleX, SkewY, SkewX, ScaleY, TransX, TransY)`.

**Files:**
- Modify: `src/iTextSharp/text/pdf/PdfContentByte.cs:2847`

- [ ] **Step 1: Adicionar o using do SkiaSharp**

Localize o topo do arquivo:

```csharp
using System;
using System.Collections;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.intern;
```

Substitua por (acrescentando o using):

```csharp
using System;
using System.Collections;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.intern;
using SkiaSharp;
```

- [ ] **Step 2: Substituir o método Transform**

Localize:

```csharp
        public void Transform(System.Drawing.Drawing2D.Matrix tx) {
            float[] c = tx.Elements;
            ConcatCTM(c[0], c[1], c[2], c[3], c[4], c[5]);
        }
```

Substitua por:

```csharp
        public void Transform(SKMatrix tx) {
            ConcatCTM(tx.ScaleX, tx.SkewY, tx.SkewX, tx.ScaleY, tx.TransX, tx.TransY);
        }
```

- [ ] **Step 3: Compilar**

Run: `dotnet build src/itextsharp.csproj -c Debug`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/iTextSharp/text/pdf/PdfContentByte.cs
git commit -m "refactor: PdfContentByte.Transform usa SKMatrix em vez de GDI Matrix"
```

---

## Task 3: Migrar Image.GetInstance para SKBitmap

As 3 sobrecargas que recebem `System.Drawing.Image` passam a receber `SKBitmap`. A leitura de pixels usa `SKColor` (propriedades `.Alpha`, `.Red`, `.Green`, `.Blue`), e um helper `ToArgb` reproduz o antigo `System.Drawing.Color.ToArgb()`.

**Files:**
- Modify: `src/iTextSharp/text/Image.cs:549-758`

- [ ] **Step 1: Adicionar o using do SkiaSharp**

Localize o topo:

```csharp
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.codec;
using System;
using System.IO;
using System.Net;
using System.Reflection;
```

Substitua por:

```csharp
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.codec;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using SkiaSharp;
```

- [ ] **Step 2: Substituir a sobrecarga com ImageFormat**

Localize:

```csharp
        public static Image GetInstance(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, format);
            return GetInstance(ms.ToArray());
        }
```

Substitua por:

```csharp
        public static Image GetInstance(SKBitmap image)
        {
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                return GetInstance(data.ToArray());
            }
        }

        private static int ToArgb(SKColor c)
        {
            return (c.Alpha << 24) | (c.Red << 16) | (c.Green << 8) | c.Blue;
        }
```

- [ ] **Step 3: Trocar a assinatura da sobrecarga forceBW e o cast do bitmap**

Localize:

```csharp
        public static Image GetInstance(System.Drawing.Image image, Color color, bool forceBW)
        {
            System.Drawing.Bitmap bm = (System.Drawing.Bitmap)image;
```

Substitua por:

```csharp
        public static Image GetInstance(SKBitmap image, Color color, bool forceBW)
        {
            SKBitmap bm = image;
```

- [ ] **Step 4: Substituir todas as chamadas `.ToArgb()`**

Use replace_all para trocar todas as ocorrências de:

```csharp
bm.GetPixel(i, j).ToArgb()
```

por:

```csharp
ToArgb(bm.GetPixel(i, j))
```

(São 6 ocorrências, nas linhas que checam transparência e extraem RGB.)

- [ ] **Step 5: Substituir as leituras de alpha `.A`**

Use replace_all para trocar todas as ocorrências de:

```csharp
int alpha = bm.GetPixel(i, j).A;
```

por:

```csharp
int alpha = bm.GetPixel(i, j).Alpha;
```

(São 2 ocorrências.)

- [ ] **Step 6: Trocar a assinatura da sobrecarga com Color**

Localize:

```csharp
        public static Image GetInstance(System.Drawing.Image image, Color color)
        {
            return Image.GetInstance(image, color, false);
        }
```

Substitua por:

```csharp
        public static Image GetInstance(SKBitmap image, Color color)
        {
            return Image.GetInstance(image, color, false);
        }
```

- [ ] **Step 7: Compilar**

Run: `dotnet build src/itextsharp.csproj -c Debug`
Expected: BUILD SUCCEEDED

- [ ] **Step 8: Commit**

```bash
git add src/iTextSharp/text/Image.cs
git commit -m "refactor: Image.GetInstance aceita SKBitmap em vez de System.Drawing.Image"
```

---

## Task 4: Migrar todos os barcodes (CreateDrawingImage → SKBitmap)

O método abstrato em `Barcode.cs` e todas as implementações precisam mudar **juntos** (a assinatura abstrata e os overrides têm que casar para compilar). Cada barcode converte os parâmetros `System.Drawing.Color` para `SKColor` uma vez no topo do método e usa `SKBitmap.SetPixel`. Os parâmetros permanecem `System.Drawing.Color` (decisão do spec, seção 5.2).

**Files:**
- Modify: `src/iTextSharp/text/pdf/Barcode.cs:428`
- Modify: `src/iTextSharp/text/pdf/Barcode39.cs`
- Modify: `src/iTextSharp/text/pdf/Barcode128.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodeInter25.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodeEAN.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodeEANSUPP.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodeCodabar.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodePostnet.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodePDF417.cs`
- Modify: `src/iTextSharp/text/pdf/BarcodeDatamatrix.cs`

- [ ] **Step 1: Barcode.cs — using + método abstrato**

Localize o topo:

```csharp
using System;
using iTextSharp.text;
```

Substitua por:

```csharp
using System;
using iTextSharp.text;
using SkiaSharp;
```

Localize:

```csharp
        public abstract System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background);
```

Substitua por:

```csharp
        public abstract SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background);
```

- [ ] **Step 2: Barcode39.cs**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            String bCode = code;
            if (extended)
                bCode = GetCode39Ex(code);
            if (generateChecksum)
                bCode += GetChecksum(bCode);
            int len = bCode.Length + 2;
            int nn = (int)n;
            int fullWidth = len * (6 + 3 * nn) + (len - 1);
            int height = (int)barHeight;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fullWidth, height);
            byte[] bars = GetBarsCode39(bCode);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : nn);
                    System.Drawing.Color c = background;
                    if (print)
                        c = foreground;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            String bCode = code;
            if (extended)
                bCode = GetCode39Ex(code);
            if (generateChecksum)
                bCode += GetChecksum(bCode);
            int len = bCode.Length + 2;
            int nn = (int)n;
            int fullWidth = len * (6 + 3 * nn) + (len - 1);
            int height = (int)barHeight;
            SKBitmap bmp = new SKBitmap(fullWidth, height);
            byte[] bars = GetBarsCode39(bCode);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : nn);
                    SKColor c = bg;
                    if (print)
                        c = fg;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

- [ ] **Step 3: Barcode128.cs**

> Este método contém uma linha com caractere especial (`code.IndexOf('￿')`). Para evitar problemas de casamento, faça **3 edições pontuais** em vez de substituir o método inteiro.

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Edição 3a — assinatura + conversão de cor. Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            String bCode;
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            String bCode;
```

Edição 3b — criação do bitmap. Localize:

```csharp
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fullWidth, height);
```

Substitua por:

```csharp
            SKBitmap bmp = new SKBitmap(fullWidth, height);
```

Edição 3c — bloco de cor no laço. Localize:

```csharp
                    System.Drawing.Color c = background;
                    if (print)
                        c = foreground;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
```

Substitua por:

```csharp
                    SKColor c = bg;
                    if (print)
                        c = fg;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
```

- [ ] **Step 4: BarcodeInter25.cs**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            String bCode = KeepNumbers(code);
            if (generateChecksum)
                bCode += GetChecksum(bCode);
            int len = bCode.Length;
            int nn = (int)n;
            int fullWidth = len * (3 + 2 * nn) + (6 + nn );
            byte[] bars = GetBarsInter25(bCode);
            int height = (int)barHeight;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fullWidth, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : nn);
                    System.Drawing.Color c = background;
                    if (print)
                        c = foreground;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            String bCode = KeepNumbers(code);
            if (generateChecksum)
                bCode += GetChecksum(bCode);
            int len = bCode.Length;
            int nn = (int)n;
            int fullWidth = len * (3 + 2 * nn) + (6 + nn );
            byte[] bars = GetBarsInter25(bCode);
            int height = (int)barHeight;
            SKBitmap bmp = new SKBitmap(fullWidth, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : nn);
                    SKColor c = bg;
                    if (print)
                        c = fg;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

- [ ] **Step 5: BarcodeEAN.cs**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize (a partir da assinatura até o `return bmp;` final do método):

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            int width = 0;
            byte[] bars = null;
            switch (codeType) {
```

Substitua a linha da assinatura por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            int width = 0;
            byte[] bars = null;
            switch (codeType) {
```

Em seguida, no mesmo método, localize:

```csharp
            int height = (int)barHeight;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = bars[k];
                    System.Drawing.Color c = background;
                    if (print)
                        c = foreground;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
            int height = (int)barHeight;
            SKBitmap bmp = new SKBitmap(width, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = bars[k];
                    SKColor c = bg;
                    if (print)
                        c = fg;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

- [ ] **Step 6: BarcodeEANSUPP.cs (só a assinatura — o corpo lança exceção)**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            throw new InvalidOperationException("The two barcodes must be composed externally.");
        }
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            throw new InvalidOperationException("The two barcodes must be composed externally.");
        }
```

- [ ] **Step 7: BarcodeCodabar.cs**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            String fullCode = code;
            if (generateChecksum && checksumText)
                fullCode = CalculateChecksum(code);
            if (!startStopText)
                fullCode = fullCode.Substring(1, fullCode.Length - 2);
            byte[] bars = GetBarsCodabar(generateChecksum ? CalculateChecksum(code) : code);
            int wide = 0;
            for (int k = 0; k < bars.Length; ++k) {
                wide += (int)bars[k];
            }
            int narrow = bars.Length - wide;
            int fullWidth = narrow + wide * (int)n;
            int height = (int)barHeight;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fullWidth, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : (int)n);
                    System.Drawing.Color c = background;
                    if (print)
                        c = foreground;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            String fullCode = code;
            if (generateChecksum && checksumText)
                fullCode = CalculateChecksum(code);
            if (!startStopText)
                fullCode = fullCode.Substring(1, fullCode.Length - 2);
            byte[] bars = GetBarsCodabar(generateChecksum ? CalculateChecksum(code) : code);
            int wide = 0;
            for (int k = 0; k < bars.Length; ++k) {
                wide += (int)bars[k];
            }
            int narrow = bars.Length - wide;
            int fullWidth = narrow + wide * (int)n;
            int height = (int)barHeight;
            SKBitmap bmp = new SKBitmap(fullWidth, height);
            for (int h = 0; h < height; ++h) {
                bool print = true;
                int ptr = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    int w = (bars[k] == 0 ? 1 : (int)n);
                    SKColor c = bg;
                    if (print)
                        c = fg;
                    print = !print;
                    for (int j = 0; j < w; ++j)
                        bmp.SetPixel(ptr++, h, c);
                }
            }
            return bmp;
        }
```

- [ ] **Step 8: BarcodePostnet.cs (usa foreground/background direto no SetPixel)**

Adicione `using SkiaSharp;` após `using iTextSharp.text;` no topo do arquivo.

Localize:

```csharp
        public override System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            int barWidth = (int)x;
```

Substitua por:

```csharp
        public override SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            int barWidth = (int)x;
```

Localize:

```csharp
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, barTall);
            int seg1 = barTall - barShort;
            for (int i = 0; i < seg1; ++i) {
                int idx = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    bool dot = (bars[k] == flip);
                    for (int j = 0; j < barDistance; ++j) {
                        bmp.SetPixel(idx++, i, (dot && j < barWidth) ? foreground : background);
                    }
                }
            }
            for (int i = seg1; i < barTall; ++i) {
                int idx = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    for (int j = 0; j < barDistance; ++j) {
                        bmp.SetPixel(idx++, i, (j < barWidth) ? foreground : background);
                    }
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
            SKBitmap bmp = new SKBitmap(width, barTall);
            int seg1 = barTall - barShort;
            for (int i = 0; i < seg1; ++i) {
                int idx = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    bool dot = (bars[k] == flip);
                    for (int j = 0; j < barDistance; ++j) {
                        bmp.SetPixel(idx++, i, (dot && j < barWidth) ? fg : bg);
                    }
                }
            }
            for (int i = seg1; i < barTall; ++i) {
                int idx = 0;
                for (int k = 0; k < bars.Length; ++k) {
                    for (int j = 0; j < barDistance; ++j) {
                        bmp.SetPixel(idx++, i, (j < barWidth) ? fg : bg);
                    }
                }
            }
            return bmp;
        }
```

- [ ] **Step 9: BarcodePDF417.cs (método virtual; usa foreground/background direto)**

Adicione `using SkiaSharp;` ao bloco de usings no topo do arquivo.

Localize:

```csharp
        public virtual System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            PaintCode();
            int h = (int)yHeight;
            int stride = (bitColumns + 7) / 8;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(bitColumns, codeRows * h);
            int y = 0;
            for (int k = 0; k < codeRows; ++k) {
                for (int hh = 0; hh < h; ++hh) {
                    int p = k * stride;
                    for (int j = 0; j < bitColumns; ++j) {
                        int b = outBits[p + (j / 8)] & 0xff;
                        b <<= j % 8;
                        bmp.SetPixel(j, y, (b & 0x80) == 0 ? background : foreground);
                    }
                    ++y;
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
        public virtual SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            PaintCode();
            int h = (int)yHeight;
            int stride = (bitColumns + 7) / 8;
            SKBitmap bmp = new SKBitmap(bitColumns, codeRows * h);
            int y = 0;
            for (int k = 0; k < codeRows; ++k) {
                for (int hh = 0; hh < h; ++hh) {
                    int p = k * stride;
                    for (int j = 0; j < bitColumns; ++j) {
                        int b = outBits[p + (j / 8)] & 0xff;
                        b <<= j % 8;
                        bmp.SetPixel(j, y, (b & 0x80) == 0 ? bg : fg);
                    }
                    ++y;
                }
            }
            return bmp;
        }
```

- [ ] **Step 10: BarcodeDatamatrix.cs (método virtual; usa foreground/background direto)**

Adicione `using SkiaSharp;` ao bloco de usings no topo do arquivo.

Localize:

```csharp
        public virtual System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            if (image == null)
                return null;
            int h = height + 2 * ws;
            int w = width + 2 * ws;
            int stride = (w + 7) / 8;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(w, h);
            for (int k = 0; k < h; ++k) {
                int p = k * stride;
                for (int j = 0; j < w; ++j) {
                    int b = image[p + (j / 8)] & 0xff;
                    b <<= j % 8;
                    bmp.SetPixel(j, k, (b & 0x80) == 0 ? background : foreground);
                }
            }
            return bmp;
        }
```

Substitua por:

```csharp
        public virtual SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background) {
            if (image == null)
                return null;
            SKColor fg = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            SKColor bg = new SKColor(background.R, background.G, background.B, background.A);
            int h = height + 2 * ws;
            int w = width + 2 * ws;
            int stride = (w + 7) / 8;
            SKBitmap bmp = new SKBitmap(w, h);
            for (int k = 0; k < h; ++k) {
                int p = k * stride;
                for (int j = 0; j < w; ++j) {
                    int b = image[p + (j / 8)] & 0xff;
                    b <<= j % 8;
                    bmp.SetPixel(j, k, (b & 0x80) == 0 ? bg : fg);
                }
            }
            return bmp;
        }
```

- [ ] **Step 11: Compilar (tudo junto)**

Run: `dotnet build src/itextsharp.csproj -c Debug`
Expected: BUILD SUCCEEDED (abstrato + todos os overrides/virtuais agora retornam `SKBitmap`).

- [ ] **Step 12: Commit**

```bash
git add src/iTextSharp/text/pdf/Barcode.cs src/iTextSharp/text/pdf/Barcode39.cs src/iTextSharp/text/pdf/Barcode128.cs src/iTextSharp/text/pdf/BarcodeInter25.cs src/iTextSharp/text/pdf/BarcodeEAN.cs src/iTextSharp/text/pdf/BarcodeEANSUPP.cs src/iTextSharp/text/pdf/BarcodeCodabar.cs src/iTextSharp/text/pdf/BarcodePostnet.cs src/iTextSharp/text/pdf/BarcodePDF417.cs src/iTextSharp/text/pdf/BarcodeDatamatrix.cs
git commit -m "refactor: barcodes geram SKBitmap em vez de System.Drawing.Bitmap"
```

---

## Task 5: Remover dependências GDI+/Windows do projeto

Neste ponto nenhum código usa mais GDI+. Removemos o `Microsoft.Windows.Compatibility`, o analisador do Upgrade Assistant, as flags de Windows Forms e a `Reference` de `System.Drawing`. `System.Drawing.Color`/`Point` continuam disponíveis via `System.Drawing.Primitives` (parte do contrato netstandard2.0).

**Files:**
- Modify: `src/itextsharp.csproj`

- [ ] **Step 1: Remover as flags de Windows Forms**

Localize:

```xml
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <UseWindowsForms>true</UseWindowsForms>
    <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>
  </PropertyGroup>
```

Substitua por:

```xml
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
```

- [ ] **Step 2: Remover a Reference de System.Drawing**

Localize:

```xml
    <Reference Update="System.Drawing">
      <Name>System.Drawing</Name>
    </Reference>
```

Apague esse bloco inteiro (deixe as `Reference` de System, System.Data e System.Xml intactas).

- [ ] **Step 3: Remover os pacotes GDI/Windows e Upgrade Assistant**

Localize:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers" Version="0.4.421302">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Windows.Compatibility" Version="7.0.5" />
    <PackageReference Include="SkiaSharp" Version="3.119.4" />
  </ItemGroup>
```

Substitua por:

```xml
  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="3.119.4" />
  </ItemGroup>
```

- [ ] **Step 4: Compilar e confirmar que não há resíduo GDI**

Run: `dotnet build src/itextsharp.csproj -c Debug`
Expected: BUILD SUCCEEDED (sem `Microsoft.Windows.Compatibility`; `System.Drawing.Color`/`Point` resolvidos via netstandard2.0).

> Se a build falhar com erro de tipo não encontrado para `System.Drawing.Color`/`Point`, adicione ao `ItemGroup` do SkiaSharp:
> `<PackageReference Include="System.Drawing.Primitives" Version="4.3.0" />`
> e compile de novo.

- [ ] **Step 5: Commit**

```bash
git add src/itextsharp.csproj
git commit -m "build: remove Microsoft.Windows.Compatibility e flags de WinForms (GDI eliminado)"
```

---

## Task 6: Atualizar o projeto de testes para net10.0

**Files:**
- Modify: `UnitTests/UnitTests.csproj`

- [ ] **Step 1: Trocar o TargetFramework e adicionar SkiaSharp**

Localize:

```xml
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>

    <IsPackable>false</IsPackable>
  </PropertyGroup>
```

Substitua por:

```xml
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>

    <IsPackable>false</IsPackable>
  </PropertyGroup>
```

Localize:

```xml
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
```

Substitua por:

```xml
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="SkiaSharp" Version="3.119.4" />
  </ItemGroup>
```

- [ ] **Step 2: Rodar a suíte de testes existente (regressão)**

Run: `dotnet test UnitTests/UnitTests.csproj -c Debug`
Expected: BUILD SUCCEEDED e todos os testes existentes PASS (o core ficou intacto; valida que a migração não quebrou nada).

- [ ] **Step 3: Commit**

```bash
git add UnitTests/UnitTests.csproj
git commit -m "test: projeto de testes migrado para net10.0 + SkiaSharp"
```

---

## Task 7: Adicionar testes da nova API SkiaSharp

Testes para: `Image.GetInstance(SKBitmap)`, `Image.GetInstance(SKBitmap, Color)` (caminho de leitura de pixels), `Barcode39.CreateDrawingImage` (retorno `SKBitmap`) e `PdfContentByte.Transform(SKMatrix)`.

**Files:**
- Create: `UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs`

- [ ] **Step 1: Criar o arquivo de testes (falhando, antes de confirmar a build)**

Crie `UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs`:

```csharp
using System.IO;
using FluentAssertions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace UnitTests.iTextSharp.text
{
    [TestClass]
    public class SkiaSharpMigrationTests
    {
        [TestMethod]
        public void Image_GetInstance_FromSkBitmap_ReturnsImageWithSameSize()
        {
            using var bmp = new SKBitmap(10, 12);
            using (var canvas = new SKCanvas(bmp))
                canvas.Clear(SKColors.Red);

            Image img = Image.GetInstance(bmp);

            img.Should().NotBeNull();
            img.Width.Should().Be(10f);
            img.Height.Should().Be(12f);
        }

        [TestMethod]
        public void Image_GetInstance_FromSkBitmap_WithColor_ReadsPixels()
        {
            using var bmp = new SKBitmap(4, 4);
            using (var canvas = new SKCanvas(bmp))
                canvas.Clear(SKColors.Blue);

            Image img = Image.GetInstance(bmp, null);

            img.Should().NotBeNull();
            img.Width.Should().Be(4f);
            img.Height.Should().Be(4f);
        }

        [TestMethod]
        public void Barcode39_CreateDrawingImage_ReturnsSkBitmap()
        {
            var barcode = new Barcode39();
            barcode.Code = "ITEXT";
            barcode.BarHeight = 30;

            SKBitmap bmp = barcode.CreateDrawingImage(
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
            cb.Transform(SKMatrix.CreateScaleTranslation(2f, 3f, 10f, 20f));
            string content = cb.InternalBuffer.ToString();

            doc.Close();

            content.Should().Contain("cm");
        }
    }
}
```

- [ ] **Step 2: Rodar os novos testes e confirmar que passam**

Run: `dotnet test UnitTests/UnitTests.csproj -c Debug --filter SkiaSharpMigrationTests`
Expected: 4 testes PASS.

> Se `Image_GetInstance_FromSkBitmap_WithColor_ReadsPixels` falhar por alpha premultiplicado (cor distorcida em imagens com transparência), decodifique/normalize o bitmap de entrada para `SKAlphaType.Unpremul` antes de ler os pixels em `Image.GetInstance(SKBitmap, Color, bool)`. Para este teste (cor opaca) não deve ocorrer.

- [ ] **Step 3: Commit**

```bash
git add UnitTests/iTextSharp/text/SkiaSharpMigrationTests.cs
git commit -m "test: cobre a nova API SkiaSharp (Image, Barcode, Transform)"
```

---

## Task 8: Verificação final

**Files:** nenhum (apenas verificação).

- [ ] **Step 1: Build completo da solução**

Run: `dotnet build itextsharp.sln -c Debug`
Expected: BUILD SUCCEEDED (lib em netstandard2.0, testes em net10.0).

- [ ] **Step 2: Suíte de testes completa**

Run: `dotnet test UnitTests/UnitTests.csproj -c Debug`
Expected: todos os testes PASS.

- [ ] **Step 3: Confirmar ausência de GDI+ residual**

Run: `git grep -nE "System\.Drawing\.(Bitmap|Image\b|Imaging|Drawing2D)|Microsoft\.Windows\.Compatibility|UseWindowsForms" -- src`
Expected: **nenhum resultado** (apenas `System.Drawing.Color`/`Point`/`Dimension` podem aparecer, e são esperados/cross-platform).

- [ ] **Step 4: Confirmar ausência de System.Drawing.Common no grafo de dependências**

Run: `dotnet list src/itextsharp.csproj package --include-transitive`
Expected: NÃO aparece `System.Drawing.Common` nem `Microsoft.Windows.Compatibility`; aparece `SkiaSharp 3.119.4`.

---

## Notas de release (documentar para consumidores)

- **Breaking changes:** `Image.GetInstance(System.Drawing.Image, ...)` agora aceita `SKBitmap`; `Barcode.CreateDrawingImage(...)` agora retorna `SKBitmap`; `PdfContentByte.Transform(Matrix)` agora aceita `SKMatrix`. Quem usa `byte[]`/stream/arquivo e geração normal de PDF não é afetado.
- **Native assets:** apps cross-platform precisam dos nativos do SkiaSharp (ex.: `SkiaSharp.NativeAssets.Linux` no Linux; Windows/macOS já incluídos no metapacote).
- **`ImageFormat` removido:** `GetInstance(SKBitmap)` encoda PNG (lossless) por padrão.

---

## Self-Review (preenchido pelo autor do plano)

- **Spec coverage:** csproj add/remove (Tasks 1, 5) ✓; Image.cs (Task 3) ✓; barcodes (Task 4) ✓; PdfContentByte.Transform (Task 2) ✓; testes net10.0 (Task 6) ✓; novos testes (Task 7) ✓; manter Color/Point ✓ (não tocados); critérios de aceite (Task 8) ✓; riscos (notas de release) ✓.
- **Placeholder scan:** nenhum TBD/TODO; todo passo tem código/comando concreto.
- **Type consistency:** `SKBitmap`/`SKColor`/`SKMatrix` usados de forma consistente; `ToArgb(SKColor)` definido na Task 3 e usado na mesma; parâmetros `System.Drawing.Color` mantidos em todos os barcodes.
