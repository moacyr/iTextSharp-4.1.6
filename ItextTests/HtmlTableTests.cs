using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ItextTests
{
    [TestClass]
    public class HtmlTableTests
    {
        [TestMethod]
        public void Parse_Tables()
        {
            StyleSheet styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            string strTexto = @"<p>teste bruto</p><table border=""1""><tbody><tr><td>teste</td><td>sdfsfsfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr></tbody></table>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            foreach (var element in objects)
            {
                var objParagraph = new Paragraph() { KeepTogether = false, Alignment = Element.ALIGN_JUSTIFIED };
                objParagraph.Add((IElement)element);
                var objPdfPCell = new PdfPCell { HorizontalAlignment = Element.ALIGN_JUSTIFIED, Border = 0, Padding = 0, PaddingTop = 8 };
                objPdfPCell.AddElement(objParagraph);

            }
            Assert.AreEqual(2, objects.Count);
        }

        [TestMethod]
        public void Parse_Tables_CellWidthSet()
        {
            Document doc = new Document();
            PdfWriter objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet.pdf", FileMode.Create));
            
            objWriter.SetLinearPageMode();
            doc.Open();

            StyleSheet styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            string strTexto = @"<p>teste bruto</p><table border=""1""><tbody><tr><td width=""80%"">teste</td><td width=""20%"">sdfsfsfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr></tbody></table>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            foreach (var element in objects)
            {
                var objParagraph = new Paragraph();
                objParagraph.Add((IElement)element);
                objParagraph.Alignment = Element.ALIGN_JUSTIFIED;
                objParagraph.SpacingBefore = 10;
                objParagraph.IndentationLeft = 20;
                objParagraph.FirstLineIndent = 20;
                doc.Add(objParagraph);
            }

            doc.Close();

            Assert.AreEqual(2, objects.Count);

        }



    }
}
