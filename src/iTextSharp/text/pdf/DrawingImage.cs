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
