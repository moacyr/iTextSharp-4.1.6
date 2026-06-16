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
