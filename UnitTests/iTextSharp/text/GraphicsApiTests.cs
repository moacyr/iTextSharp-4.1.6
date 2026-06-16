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
