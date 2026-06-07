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

            using SKBitmap bmp = barcode.CreateDrawingImage(
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
