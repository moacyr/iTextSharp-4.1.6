# Spec: Migração iTextSharp 4.1.6 → SkiaSharp (netstandard2.0, sem GDI)

**Data:** 2026-06-07
**Status:** Aprovado (design)
**Autor:** Moacyr Rodrigues Pereira

## 1. Contexto e motivação

A biblioteca `itextsharp` (fork 4.1.6) tem alvo `netstandard2.0`, mas depende de
GDI+ via `Microsoft.Windows.Compatibility` (7.0.5), `UseWindowsForms=true` e
`System.Drawing.Common`. Desde o .NET 7, `System.Drawing.Common` é **suportado
apenas no Windows** e lança `PlatformNotSupportedException` em Linux/macOS. Isso
impede o uso confiável da biblioteca em apps .NET 10 cross-platform/containers.

O objetivo é tornar a biblioteca **verdadeiramente portável** mantendo o alvo
`netstandard2.0`, substituindo a parte raster (GDI+) por **SkiaSharp** e
removendo a dependência de Windows.

### Descoberta-chave (fundamenta a estratégia)

Nem todo `System.Drawing` no código é o GDI+ problemático. Há dois assemblies
distintos misturados:

- **`System.Drawing.Primitives`** — cross-platform, faz parte do contrato
  `netstandard2.0`. Fornece `System.Drawing.Color` e `System.Drawing.Point`.
  **Não é problema** e será mantido.
- **`System.Drawing.Common` (GDI+)** — Windows-only. Fornece `Bitmap`, `Image`,
  `Graphics`, `Imaging.ImageFormat`, `Drawing2D.Matrix`. **É o bloqueador** e
  será substituído por SkiaSharp.

Além disso, `System.Drawing.Dimension`/`Dimension2D` usados em `Table.cs` são
**classes próprias do projeto** (`src/System/Drawing/Dimension.cs` e
`Dimension2D.cs`), não da Microsoft — sem dependência externa.

O iText também possui **decoders de imagem 100% gerenciados** (`PngImage`,
`GifImage`, `TiffImage`, `BmpImage`, `Jpeg`, `Jpeg2000`). O caminho principal de
imagens (carregar de `byte[]`/stream/arquivo/URL/base64 e inserir no PDF) **já
não usa GDI**. O GDI só aparece em métodos de conveniência.

## 2. Escopo

### Em escopo

- Remover dependências GDI+/Windows do `src/itextsharp.csproj`.
- Adicionar dependência `SkiaSharp`.
- Substituir a superfície GDI+ raster por equivalentes SkiaSharp.
- Atualizar o projeto de testes para `net10.0` e adicionar testes para a nova API.
- Manter o alvo da biblioteca em `netstandard2.0`.

### Fora de escopo

- Converter `System.Drawing.Color`/`System.Drawing.Point` para tipos SkiaSharp
  (decisão explícita: manter — são cross-platform via Primitives).
- Refatorações não relacionadas à migração.
- Multi-targeting adicional (ex.: `net10.0`) — manter apenas `netstandard2.0`.

## 3. Superfície GDI+ a substituir (inventário completo)

Confirmado por varredura. Estes são os únicos pontos que dependem de
`System.Drawing.Common`:

1. **`src/iTextSharp/text/Image.cs`** (linhas ~549–755)
   - `GetInstance(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)`
   - `GetInstance(System.Drawing.Image image, Color color, bool forceBW)` — usa
     `(System.Drawing.Bitmap)image` e `bm.GetPixel(...)` (linhas ~568, 594–700)
   - `GetInstance(System.Drawing.Image image, Color color)`

2. **`src/iTextSharp/text/pdf/Barcode.cs`** (linha 428)
   - `public abstract System.Drawing.Image CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background);`

3. **9 implementações concretas de `CreateDrawingImage`** (usam
   `new System.Drawing.Bitmap(...)` + `bmp.SetPixel(...)`):
   - `BarcodePDF417.cs` (~846), `Barcode128.cs` (~673), `BarcodeInter25.cs` (~291),
     `BarcodeDatamatrix.cs` (~763), `BarcodeEANSUPP.cs` (~146), `BarcodeEAN.cs` (~648),
     `BarcodeCodabar.cs` (~293), `Barcode39.cs` (~342), `BarcodePostnet.cs` (~176)

4. **`src/iTextSharp/text/pdf/PdfContentByte.cs`** (linha 2847)
   - `public void Transform(System.Drawing.Drawing2D.Matrix tx)` — usa `tx.Elements`

> Nota: `SetPixel`/`GetPixel` em `GifImage.cs` e `PngImage.cs` são métodos
> **próprios** sobre arrays `byte[]`/`int[]` — **não** são GDI e não mudam.

## 4. Mudanças no projeto

### 4.1 `src/itextsharp.csproj`

**Remover:**
- `<PackageReference Include="Microsoft.Windows.Compatibility" Version="7.0.5" />`
- `<PackageReference Include="Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers" ... />`
- `<UseWindowsForms>true</UseWindowsForms>`
- `<ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>`
- Os `<Reference Update="System.Drawing">` (e demais `<Reference Update>` legados,
  se inofensivos podem ser limpos).

**Adicionar:**
- `<PackageReference Include="SkiaSharp" Version="2.88.x" />`
  - Decisão: usar a linha **2.88.x** (suporte sólido e amplo a `netstandard2.0`).
    Versão exata (ex.: `2.88.9`) confirmada na implementação.
- **Fallback defensivo:** se `System.Drawing.Color`/`Point` não resolverem após
  remover o Compatibility pack, adicionar
  `<PackageReference Include="System.Drawing.Primitives" />` explicitamente.

**Manter:**
- `<TargetFramework>netstandard2.0</TargetFramework>`

### 4.2 `UnitTests/UnitTests.csproj`

- Alterar `<TargetFramework>net6.0</TargetFramework>` → `net10.0`.
- Manter os pacotes de teste; atualizar versões se necessário para compatibilidade
  com net10.0.

## 5. Mapeamento de API (GDI+ → SkiaSharp)

### 5.1 `Image.cs`

| Antes | Depois |
|---|---|
| `GetInstance(System.Drawing.Image, System.Drawing.Imaging.ImageFormat)` | `GetInstance(SKBitmap)` → `bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray()` → `GetInstance(byte[])` |
| `GetInstance(System.Drawing.Image, Color, bool)` | `GetInstance(SKBitmap, Color, bool)` — substituir `(Bitmap)image` + `bm.GetPixel(i,j)` por `SKBitmap.GetPixel(i,j)` |
| `GetInstance(System.Drawing.Image, Color)` | `GetInstance(SKBitmap, Color)` |

**Conversão de pixel:** `SKColor` expõe `.Alpha`, `.Red`, `.Green`, `.Blue`.
O equivalente a `System.Drawing.Color.ToArgb()` é:
`(skColor.Alpha << 24) | (skColor.Red << 16) | (skColor.Green << 8) | skColor.Blue`.
Garantir leitura com **alpha não-premultiplicado** (configurar/validar o
`SKColorType`/`SKAlphaType` do `SKBitmap`) para casar com a semântica GDI atual.

### 5.2 Barcodes — `CreateDrawingImage`

- Assinatura nova (em `Barcode.cs` e nas 9 concretas):
  `public [abstract|override] SKBitmap CreateDrawingImage(System.Drawing.Color foreground, System.Drawing.Color background)`
- **Decisão:** manter os parâmetros como `System.Drawing.Color` (consistente com
  a decisão de manter Color; minimiza quebra para chamadores). Converter para
  `SKColor` internamente: `new SKColor(c.R, c.G, c.B, c.A)`.
- Trocar `new System.Drawing.Bitmap(w, h)` por `new SKBitmap(w, h)` e
  `bmp.SetPixel(x, y, c)` por `bmp.SetPixel(x, y, skColor)`.
- Performance: `SetPixel` em `SKBitmap` é adequado para o tamanho pequeno de
  barcodes; otimização via pixmap é desnecessária.

### 5.3 `PdfContentByte.Transform`

- Antes: `Transform(System.Drawing.Drawing2D.Matrix tx)` usando
  `tx.Elements` = `{m11, m12, m21, m22, dx, dy}`.
- Depois: `Transform(SKMatrix tx)` mapeando:
  `ConcatCTM(tx.ScaleX, tx.SkewY, tx.SkewX, tx.ScaleY, tx.TransX, tx.TransY)`.

## 6. O que NÃO muda

- Core de geração de PDF.
- Caminho principal de imagens (`byte[]`/stream/arquivo/URL/base64) via decoders
  nativos próprios.
- `Color.cs` e toda a hierarquia de cores (`GrayColor`, `CMYKColor`, etc.).
- `Table.cs`, codecs WMF (`MetaDo.cs`, `MetaState.cs`) — `System.Drawing.Point`
  permanece.
- As classes próprias `System.Drawing.Dimension`/`Dimension2D`.

## 7. Quebra de compatibilidade (esperada e documentada)

A API pública muda **apenas** para consumidores que usavam os métodos GDI:

- `Image.GetInstance(System.Drawing.Image, ...)` → passa a aceitar `SKBitmap`.
- `barcode.CreateDrawingImage(...)` → passa a retornar `SKBitmap`.
- `PdfContentByte.Transform(Matrix)` → passa a aceitar `SKMatrix`.

Consumidores que usam `byte[]`/stream e geração normal de PDF **não são afetados**.

## 8. Testes

- `UnitTests` migrado para `net10.0`.
- Testes existentes devem continuar passando (validam que o core ficou intacto).
- Novos testes:
  - `Image.GetInstance(SKBitmap)` produz `Image` válida.
  - `barcode.CreateDrawingImage(...)` retorna `SKBitmap` com dimensões esperadas.
  - `PdfContentByte.Transform(SKMatrix)` aplica a transformação correta (verificar
    via conteúdo gerado/CTM).

## 9. Critérios de aceite (verificação)

1. `dotnet build` da biblioteca em `netstandard2.0` **sem erros**.
2. `dotnet test` do projeto de testes em `net10.0` **verde**.
3. Nenhuma referência a `System.Drawing.Common` ou `Microsoft.Windows.Compatibility`
   permanece (verificar via grep no `.csproj` e no `.deps.json` gerado).
4. Nenhum `using`/símbolo de `System.Drawing.Bitmap`/`Image`/`Imaging`/`Drawing2D`
   permanece no código.

## 10. Riscos conhecidos e mitigações

- **Native assets do SkiaSharp:** apps cross-platform precisam dos pacotes de
  nativos (ex.: `SkiaSharp.NativeAssets.Linux` no Linux; macOS/Windows incluídos
  no metapacote). Documentar nas notas de release.
- **`ImageFormat` removido:** as sobrecargas passam a encodar **PNG (lossless)**
  por padrão; comportamento ligeiramente diferente de quem passava JPEG (lossy).
  Documentar.
- **Semântica de alpha:** premultiplied vs straight ao ler pixels — validar com
  teste de imagem com transparência.

## 11. Sequência de implementação sugerida

1. Atualizar `src/itextsharp.csproj` (remover GDI/Windows, adicionar SkiaSharp).
2. Migrar `PdfContentByte.Transform` (menor e isolado).
3. Migrar `Image.cs` (sobrecargas `GetInstance`).
4. Migrar `Barcode.cs` (abstrato) + 9 barcodes concretos.
5. Atualizar `UnitTests` para `net10.0` e adicionar testes da nova API.
6. Build + test + verificação dos critérios de aceite.
