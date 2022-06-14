using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.iTextSharp.text.Html
{
    [TestClass]
    public class HtmlTableTests
    {
        [TestMethod]
        public void Parse_Tables()
        {
            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto =
                @"<p>teste bruto</p><table border=""1""><tbody><tr><td>teste</td><td>sdfsfsfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr></tbody></table>";
            var objects = HTMLWorker.ParseToList(new StringReader(strTexto), styles);
            foreach (var element in objects)
            {
                var objParagraph = new Paragraph { KeepTogether = false, Alignment = Element.ALIGN_JUSTIFIED };
                objParagraph.Add((IElement)element);
                var objPdfPCell = new PdfPCell { HorizontalAlignment = Element.ALIGN_JUSTIFIED, Border = 0, Padding = 0, PaddingTop = 8 };
                objPdfPCell.AddElement(objParagraph);
            }
            Assert.AreEqual(2, objects.Count);
        }

        [TestMethod]
        public void Parse_Tables_CellWidthSet()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto =
                @"<p>teste bruto</p><table border=""1""><tbody><tr><td width=""80%"">teste</td><td width=""20%"">sdfsfsfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr><tr><td>sfsdfsdfsdfsdfsdfsdfsdf</td><td>sdfsdfsdfsdfsdfsdfsdfsdf</td></tr></tbody></table>";
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

        [TestMethod]
        public void Parse_Tables_CellWidthSet_Colspan()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_Colspan.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto =
                @"<p>teste bruto</p><table border=""1""><tbody><tr><td width=""50%"" colspan=""2"">teste</td><td width=""50%"">sdfsfsfsdf</td></tr><tr><td>col 1</td><td>col 2</td><td>col 3</td></tr></tbody></table>";
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

        [TestMethod]
        public void Parse_Tables_CellWidthSet_Rowspan()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_Rowspan.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto =
                @"<p>teste bruto</p><table border=""1""><tbody><tr><td width=""50%"" rowspan=""2"">teste</td><td width=""30%"">sdfsfsfsdf</td><td width=""20%"">sdfsfsfsdf</td></tr><tr><td>col 2</td><td></td></tr></tbody></table>";
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

            var celulas = objects.ToArray().Where(x => x.GetType() == typeof(PdfPTable)).Cast<PdfPTable>().SelectMany(x => x.Rows.ToArray()).Cast<PdfPRow>().SelectMany(x => x.GetCells())
                .Where(x => x != null);

            Assert.AreEqual(1, celulas.Where(x => x.Rowspan == 2).Count());

            Assert.AreEqual(2, objects.Count);

            Assert.AreEqual(3, celulas.Select(x => x.Width).Distinct().Count());
        }

        [TestMethod]
        public void Parse_Tables_CellWidthSet_Px()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_Px.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto = @"<p>Tabela teste subse&ccedil;&otilde;es</p><p>&nbsp;</p><table border=""1"" cellspacing=""0"" cellpadding=""0"" width=""276""><tbody><tr><td valign=""top"" width=""69""><p align=""center"">1</p></td><td valign=""top"" width=""69""><p align=""center"">3</p></td><td valign=""top"" width=""69""><p>445</p></td>
<td valign=""top"" width=""69""><p>ljkhjh</p></td></tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td><td valign=""top"" width=""69""><p>1122</p></td><td valign=""top"" width=""69""><p>kji252</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p>
</td></tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td><td valign=""top"" width=""69""><p>1123</p></td><td valign=""top"" width=""69""><p>kji253</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p></td></tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td>
<td valign=""top"" width=""69""><p>1124</p></td><td valign=""top"" width=""69""><p>kji254</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p></td></tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td><td valign=""top"" width=""69""><p>1125</p></td><td valign=""top"" width=""69"">
<p>kji255</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p></td></tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td><td valign=""top"" width=""69""><p>1126</p></td><td valign=""top"" width=""69""><p>kji256</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p></td>
</tr><tr><td valign=""top"" width=""69""><p align=""center"">g</p></td><td valign=""top"" width=""69""><p>1127</p></td><td valign=""top"" width=""69""><p>kji257</p></td><td valign=""top"" width=""69""><p align=""center"">oll</p></td></tr></tbody></table>";
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

            Assert.AreEqual(3, objects.Count);
        }


        [TestMethod]
        public void Parse_Tables_CellWidthSet_TableInsideCell()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_TableInsideCell.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto = @"<table style=""width: 70%; float: left;""border=""1""><tbody><tr><td style=""width: 80%; text-align: center;""colspan=""2"">Blablabla titulo 1</td><td>Detalhe titulo 2</td></tr><tr><td colspan=""2""><table style=""border-color: red; background-color: gray; width: 100%;""border=""1""><tbody><tr><td style=""width: 10%;"">Peq</td><td>Grande</td></tr></tbody></table></td><td>coluna 3</td></tr><tr><td>col 1</td><td style=""text-align: right;"">col2</td><td style=""text-align: center;"">col3</td></tr></tbody></table>";
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

            Assert.AreEqual(1, objects.Count);
        }


        [TestMethod]
        public void Parse_Tables_CellWidthSet_TableWidthZero()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_TableWidthZero.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto = @"<table width=""0""><tbody><tr style=""height: 45px;""><td style=""height: 45px;"" width=""132""><p><strong>NOME</strong></p></td><td style=""height: 45px;"" width=""115""><p><strong>SETOR</strong></p></td><td style=""height: 45px;"" width=""134""><p><strong>FUN&Ccedil;&Atilde;O</strong></p></td><td style=""height: 45px;"" width=""115""><p><strong>AGENTE</strong></p></td></tr><tr style=""height: 80px;""><td style=""height: 80px;"" width=""132""><p>Karem Pereira Costa</p></td><td style=""height: 80px;"" width=""115""><p>Recep&ccedil;&atilde;o</p></td><td style=""height: 80px;"" width=""134""><p>Auxiliar Administrativo</p></td><td style=""height: 80px;"" width=""115""><p>&nbsp; &nbsp;V&iacute;rus, Bact&eacute;rias e Fungos</p></td></tr><tr style=""height: 62px;""><td style=""height: 62px;"" width=""132""><p>Luciana Pinheiro</p></td><td style=""height: 62px;"" width=""115""><p>Marketing</p></td><td style=""height: 62px;"" width=""134""><p>Promotora de Vendas</p></td><td style=""height: 62px;"" width=""115""><p>-</p></td></tr><tr style=""height: 112px;""><td style=""height: 112px;"" width=""132""><p>Maria Aparecida Pereira Santos Alves</p></td><td style=""height: 112px;"" width=""115""><p>Servi&ccedil;os Gerais</p></td><td style=""height: 112px;"" width=""134""><p>Auxiliar de Servi&ccedil;os Gerais</p></td><td style=""height: 112px;"" width=""115""><p>Produtos Sapon&aacute;ceos</p><p>V&iacute;rus, Bact&eacute;rias e Fungos</p></td></tr><tr style=""height: 62px;""><td style=""height: 62px;"" width=""132""><p>Paulo Roberto de Jesus</p></td><td style=""height: 62px;"" width=""115""><p>Administrativo</p></td><td style=""height: 62px;"" width=""134""><p>Auxiliar Administrativo</p></td><td style=""height: 62px;"" width=""115""><p>V&iacute;rus, Bact&eacute;rias e Fungos</p></td></tr><tr style=""height: 62px;""><td style=""height: 62px;"" width=""132""><p>Sabrina Cardoso Correa Dutra</p></td><td style=""height: 62px;"" width=""115""><p>Marketing</p></td><td style=""height: 62px;"" width=""134""><p>Promotora de Vendas</p></td><td style=""height: 62px;"" width=""115""><p>&nbsp; &nbsp; &nbsp;-</p></td></tr><tr style=""height: 68.6px;""><td style=""height: 68.6px;"" width=""132""><p>Sebastiana do Ros&aacute;rio Soares</p></td><td style=""height: 68.6px;"" width=""115""><p>Servi&ccedil;os Gerais</p></td><td style=""height: 68.6px;"" width=""134""><p>Auxiliar de Servi&ccedil;os Gerais</p></td><td style=""height: 68.6px;"" width=""115""><p>Produtos Sapon&aacute;ceos</p><p>V&iacute;rus, Bact&eacute;rias e Fungos</p></td></tr><tr style=""height: 62px;""><td style=""height: 62px;"" width=""132""><p>Vanessa Santos de Oliveira Alves</p></td><td style=""height: 62px;"" width=""115""><p>Recep&ccedil;&atilde;o</p></td><td style=""height: 62px;"" width=""134""><p>Supervisor (a) Administrativo</p></td><td style=""height: 62px;"" width=""115""><p>V&iacute;rus, Bact&eacute;rias e Fungos</p></td></tr></tbody></table><p>&nbsp;</p>";
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

        [TestMethod]
        public void Parse_Tables_Varias_Linhas_E_Colunas_Mescladas()
        {
            var doc = new Document();
            var objWriter = PdfWriter.GetInstance(doc, new FileStream("Parse_Tables_CellWidthSet_TableWidthZero.pdf", FileMode.Create));

            objWriter.SetLinearPageMode();
            doc.Open();

            var styles = new StyleSheet();
            styles.LoadTagStyle(HtmlTags.UNORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.LISTITEM, "leading", "20");
            styles.LoadTagStyle(HtmlTags.ORDEREDLIST, "indent", "20");
            styles.LoadTagStyle(HtmlTags.PARAGRAPH, HtmlTags.TOPMARGIN, "40");

            var strTexto = "<table style=\"width: 564px; \" border=\"1\" cellspacing=\"0\" cellpadding=\"0\"><tbody><tr><td width=\"156\"><p align=\"center\"><strong>Fun&ccedil;&otilde;es</strong></p></td><td width=\"108\"><p align=\"center\"><strong>M&aacute;quina, equipamento, ve&iacute;culo etc</strong></p></td><td width=\"96\"><p align=\"center\"><strong>N&iacute;vel de ru&iacute;do</strong></p><p align=\"center\"><strong>em dB (A)</strong></p></td><td width=\"84\"><p align=\"center\"><strong>Tempo de exposi&ccedil;&atilde;o di&aacute;ria</strong></p></td><td width=\"120\"><p align=\"center\"><strong>M&aacute;xima exposi&ccedil;&atilde;o di&aacute;ria permitida</strong></p></td></tr><tr><td rowspan=\"7\" width=\"156\"><p align=\"center\">Motorista</p><p align=\"center\">e</p><p align=\"center\">Operador Munk</p></td><td valign=\"top\" width=\"108\"><p align=\"center\">Caminh&otilde;es</p></td><td colspan=\"3\" rowspan=\"2\" valign=\"top\" width=\"300\"><p align=\"center\">&nbsp;</p></td></tr><tr><td valign=\"top\" width=\"108\"><p align=\"center\">Mercedez Benz 1420</p></td></tr><tr><td valign=\"top\" width=\"108\"><p align=\"center\">Lenta</p></td><td valign=\"top\" width=\"96\"><p align=\"center\">82.0</p></td><td valign=\"top\" width=\"84\"><p align=\"center\">1 hora</p></td><td valign=\"top\" width=\"120\"><p align=\"center\">8 horas</p></td></tr><tr><td valign=\"top\" width=\"108\"><p align=\"center\">1.200 a 1.500 giros</p></td><td valign=\"top\" width=\"96\"><p align=\"center\">90.0</p></td><td valign=\"top\" width=\"84\"><p align=\"center\">2 a 3 horas</p></td><td valign=\"top\" width=\"120\"><p align=\"center\">4 horas</p></td></tr><tr><td valign=\"top\"><p align=\"center\">Mercedez Benz 710</p></td><td colspan=\"3\" valign=\"top\"><p align=\"center\">&nbsp;</p></td></tr><tr><td valign=\"top\"><p align=\"center\">Lenta</p></td><td valign=\"top\"><p align=\"center\">86</p></td><td valign=\"top\"><p align=\"center\">1 hora</p></td><td valign=\"top\"><p align=\"center\">7 horas</p></td></tr><tr><td valign=\"top\"><p align=\"center\">1.200 &aacute; 1.500 giros</p></td><td valign=\"top\"><p align=\"center\">96.0</p></td><td valign=\"top\"><p align=\"center\">2 a 3 horas</p></td><td valign=\"top\"><p align=\"center\">1 hora e 45 minutos</p></td></tr></tbody></table>";
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

            Assert.AreEqual(1, objects.Count);
        }
    }
}