using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text.pdf;

namespace iTextSharp.text.html.simpleparser
{
    public class InlineStyle
    {
        /// <summary>
        /// Parse the style values and add to props
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cell"></param>
        /// <param name="props"></param>
        /// <param name="htmlTag"></param>
        public void StyleCell(string value, PdfPCell cell, out float width)
        {
            var attribs = value.Split(';');
            width = 0;
            foreach (var attrib in attribs.Where(x => !string.IsNullOrEmpty(x)))
            {
                var kv = attrib.Split(':');

                if (kv.Length > 1)
                    kv[1] = kv[1].Trim();

                switch (kv[0].Trim())
                {
                    case "width":
                        float num;
                        var isNumber = float.TryParse(kv[1].Substring(0, kv[1].Length - 1), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out num);
                        if (isNumber)
                        {
                            width = num / 100f;
                        }
                        break;
                    case "background-color":
                        cell.BackgroundColor = Markup.DecodeColor(kv[1]);
                        break;
                    case "border-color":
                        cell.BorderColor = Markup.DecodeColor(kv[1]);
                        break;
                }
            }
        }
    }
}
