using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;

namespace ItextTests
{
    [TestClass]
    public class HtmlWorkerTests
    {
        [TestMethod]
        public void Parse_EmptyParagraph_ComEntity()
        {
            StyleSheet styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            string strTexto = "\r\n<p>&nbsp;</p>\r\n<p>Setor: Vendas</p>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            Assert.AreEqual(2,objects.Count);

            strTexto = "<font face=\"times-roman\" size=\"3\"><p>As medi&ccedil;&otilde;es foram feitas nos locais de perman&ecirc;ncia dos trabalhadores, junto&nbsp;&aacute;&nbsp;fonte ruidosa. Utilizou-se o medidor de press&atilde;o sonoro &ndash; Marca Soud Level Meter; modeloDT-805A; fabricado pela CI Hiseg Instrumentos. Os n&iacute;veis de ru&iacute;do foram mensurados com instrumento (decibel&iacute;metro) operando nos circuitos de compensa&ccedil;&atilde;o &ldquo;A&rdquo; e de resposta lenta (SLOW). As leituras foram realizadas pr&oacute;ximo ao ouvido do trabalhador. O ru&iacute;do encontrado no ambiente de trabalho est&aacute; abaixo de 80 decib&eacute;is&nbsp;&eacute;&nbsp;n&atilde;o necess&aacute;rio realizar uma dosimetria de ru&iacute;do, pois n&atilde;o&nbsp;&eacute;&nbsp;prejudicial&nbsp;&aacute;&nbsp;sa&uacute;de do trabalhador.</p>\r\n<p>&nbsp;</p>\r\n<p>Setor: Vendas</p>\r\n<p>Fun&ccedil;&atilde;o: Servi&ccedil;os Gerais</p>\r\n<p>N&iacute;vel m&iacute;nimo: 56 dB (A)</p>\r\n<p>N&iacute;vel m&aacute;ximo: 63 dB (A)&nbsp;</p></font>";
            objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            Assert.AreEqual(6,objects.Count);
        }

        [TestMethod]
        public void Parse_EmptyParagraph_ComEspaço()
        {
            StyleSheet styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            string strTexto = "\r\n<p> </p>\r\n<p>Setor: Vendas</p>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);

            Assert.AreEqual(1, objects.Count);
        }

        [TestMethod]
        public void Parse_FontSize_small()
        {
            StyleSheet styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            string strTexto = @"<p style=""font-size:xx-small;"">Absolute size - xx-small</p>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            foreach (var element in objects)
            {
                var objParagraph = new Paragraph() { KeepTogether = false, Alignment = Element.ALIGN_JUSTIFIED };
                objParagraph.Add((IElement)element);
                var objPdfPCell = new PdfPCell { HorizontalAlignment = Element.ALIGN_JUSTIFIED, Border = 0, Padding = 0, PaddingTop = 8 };
                objPdfPCell.AddElement(objParagraph);
                
            }
            Assert.AreEqual(1, objects.Count);
        }

    }
}
